using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrpcBenchmark
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new GrpcBenchmarkStack(app, "GrpcBenchmarkStack");
            app.Synth();
        }
    }
}
