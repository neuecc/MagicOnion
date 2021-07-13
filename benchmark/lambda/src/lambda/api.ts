export const ATTR_LOAD_BALANCER_ARNS = 'LoadBalancerArns'
export const ATTR_TARGET_GROUP_NAME = 'TargetGroupName'
export const ATTR_TARGET_GROUP_FULL_NAME = 'TargetGroupFullName'

export type TargetGroupProperties = Partial<{
    ServiceToken: string
    Name: string
    Port: number
    Protocol: 'HTTP' | 'HTTPS' | 'TCP' | 'TCP_UDP' | 'TLS' | 'UDP'
    ProtocolVersion: 'HTTP1' | 'HTTP2' | 'GRPC'
    VpcId: string
    TargetType: 'instance' | 'ip' | 'lambda'
    HealthCheckEnabled: 'true' | 'false'
    HealthCheckIntervalSeconds: number
    HealthCheckPath: string
    HealthCheckPort: string
    HealthCheckProtocol: 'HTTP' | 'HTTPS' | 'TCP' | 'TCP_UDP' | 'TLS' | 'UDP'
    HealthCheckTimeoutSeconds: number
    HealthyThresholdCount: number
    Matcher: { [HttpCode: string]: string }
    UnhealthyThresholdCount: number
}>