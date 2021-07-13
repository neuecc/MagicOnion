using Amazon.S3;
using Benchmark.ClientLib.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.ClientLib.Storage
{
    public interface IStorage
    {
        /// <summary>
        /// Get Reports
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string[]> Get(string path, string prefix, CancellationToken ct);
        /// <summary>
        /// List report names
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string[]> List(string path, string prefix, CancellationToken ct);
        /// <summary>
        /// Save report
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <param name="overwrite"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> Save(string path, string prefix, string name, string content, bool overwrite = false, CancellationToken ct = default);
    }

    public static class StorageFactory
    {
        private static IStorage storage;

        public static IStorage Create(ILogger logger)
        {
            if (storage != null)
                return storage;

            // todo: Google... etc...?
            if (AmazonEnvironment.IsAmazonEc2() && Environment.GetEnvironmentVariable("BENCHCLIENT_USE_S3") != "0")
            {
                var config = new AmazonS3Config()
                {
                    RegionEndpoint = Amazon.Util.EC2InstanceMetadata.Region,
                };
                storage = new AmazonS3Storage(logger, config);
            }
            else if (Environment.GetEnvironmentVariable("BENCHCLIENT_USE_S3") == "1")
            {
                var config = new AmazonS3Config()
                {
                    RegionEndpoint = Amazon.Util.EC2InstanceMetadata.Region,
                };
                storage = new AmazonS3Storage(logger, config);
            }
            else
            {
                // fall back
                storage = new LocalStorage(logger);
            }
            return storage;
        }
    }

    public class LocalStorage : IStorage
    {
        private static readonly object lockObj = new object();

        private readonly ILogger _logger;
        public LocalStorage(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get reports
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<string[]> Get(string path, string prefix, CancellationToken ct = default)
        {
            var dir = $"{path}/{prefix.TrimEnd('/')}";
            _logger?.LogDebug($"Get content from local storage {dir}");

            if (!Directory.Exists(dir))
                throw new FileNotFoundException($"Directory not found. {dir}");

            var contents = new List<string>();
            foreach (var item in Directory.EnumerateFiles(dir))
            {
                var content = File.ReadAllText(item);
                contents.Add(content);
            }
            return Task.FromResult(contents.ToArray());
        }

        /// <summary>
        /// List report names
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<string[]> List(string path, string prefix, CancellationToken ct)
        {
            var dir = $"{path}/{prefix.TrimEnd('/')}";
            _logger?.LogDebug($"listing content from local storage {dir}");

            return Task.FromResult(Directory.EnumerateFiles(dir).ToArray());
        }

        /// <summary>
        /// Save report to desired local directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <param name="overwrite"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<string> Save(string path, string prefix, string name, string content, bool overwrite = false, CancellationToken ct = default)
        {
            var dir = $"{path}/{prefix.TrimEnd('/')}";
            Directory.CreateDirectory(dir);

            var basePath = Path.Combine(dir, name);
            var savePath = Save(content, basePath, overwrite);
            return Task.FromResult(Path.GetFullPath(savePath));
        }

        private string Save(string content, string path, bool overwrite)
        {
            lock (lockObj)
            {
                var savePath = overwrite ? path : GetSafeSavePath(path, 1);

                _logger?.LogDebug($"Save content to local storage {Path.GetFullPath(savePath)}");
                File.WriteAllText(savePath, content);
                return savePath;
            }
        }

        /// <summary>
        /// Get safe filename to save. If same name found, increment index and renew filename.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="index"></param>
        /// <param name="suffixNamePattern"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        private static string GetSafeSavePath(string basePath, int index, string suffixNamePattern = "{0:00000}", string savePath = "")
        {
            if (index == 99999)
                return savePath;
            if (string.IsNullOrEmpty(savePath))
                savePath = basePath;
            if (!File.Exists(savePath))
                return savePath;

            var fileName = Path.GetFileNameWithoutExtension(basePath) + "_" + string.Format(suffixNamePattern, index) + Path.GetExtension(basePath);
            savePath = Path.Combine(Path.GetDirectoryName(basePath), fileName);
            return GetSafeSavePath(basePath, index + 1, suffixNamePattern, savePath);
        }
    }

    public class AmazonS3Storage : IStorage
    {
        private readonly ILogger _logger;
        private readonly Amazon.S3.AmazonS3Client _client;

        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public AmazonS3Storage(ILogger logger, AmazonS3Config config)
        {
            _logger = logger;
            _client = new Amazon.S3.AmazonS3Client(config);
        }

        public AmazonS3Storage(ILogger logger, AmazonS3Config config, string accessKey, string accessSecret)
        {
            _logger = logger;
            _client = new Amazon.S3.AmazonS3Client(accessKey, accessSecret, config);
        }

        /// <summary>
        /// Get report from S3 Bucket
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string[]> Get(string path, string prefix, CancellationToken ct = default)
        {
            _logger?.LogDebug($"downloading content from S3. bucket {path}, prefix {prefix}");

            // todo: continuation
            var objects = await ListObjectsAsync(path, prefix, ct);

            if (!objects.Any())
                return Array.Empty<string>();

            var contents = new ConcurrentBag<string>();
            var tasks = objects.Select(async x =>
            {
                using var res = await _client.GetObjectAsync(new Amazon.S3.Model.GetObjectRequest
                {
                    BucketName = x.BucketName,
                    Key = x.Key,
                }, ct);
                using var stream = res.ResponseStream;
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                contents.Add(content);
            });
            await Task.WhenAll(tasks);
            return contents.ToArray();
        }

        /// <summary>
        /// List report of S3 Bucket
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string[]> List(string path, string prefix, CancellationToken ct)
        {
            _logger?.LogDebug($"listing content from S3. bucket {path}, prefix {prefix}");
            var objects = await ListObjectsAsync(path, prefix, ct);
            return objects.Select(x => x.Key).ToArray();
        }

        /// <summary>
        /// Save report to S3 Bucket
        /// </summary>
        /// <param name="path">BucketName</param>
        /// <param name="prefix">Bucket Prefix</param>
        /// <param name="name">Key Name</param>
        /// <param name="content">File Content</param>
        /// <param name="overwrite"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string> Save(string path, string prefix, string name, string content, bool overwrite = false, CancellationToken ct = default)
        {
            await semaphore.WaitAsync();
            try
            {
                var basePath = $"{prefix}/{name}";
                // too many object will refected by s3 ListObjectV2 API
                //var savePath = overwrite ? basePath : await GetSafeSavePath(path, basePath, 1, ct);
                var savePath = basePath;

                _logger?.LogDebug($"uploading content to S3. bucket {path} key {savePath}");
                await _client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = path,
                    ContentBody = content,
                    Key = savePath,
                }, ct);

                return $"https://{path}.s3-ap-northeast-1.amazonaws.com/{savePath}";
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<List<Amazon.S3.Model.S3Object>> ListObjectsAsync(string bucket, string prefix, CancellationToken ct = default)
        {
            var request = new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
            };
            var objects = new List<Amazon.S3.Model.S3Object>();
            Amazon.S3.Model.ListObjectsV2Response response;

            do
            {
                response = await _client.ListObjectsV2Async(request, ct);
                objects.AddRange(response.S3Objects);
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);
            return objects;
        }
    }
}
