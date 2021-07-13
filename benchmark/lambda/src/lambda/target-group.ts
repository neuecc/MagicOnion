import {
    ATTR_LOAD_BALANCER_ARNS,
    ATTR_TARGET_GROUP_FULL_NAME,
    ATTR_TARGET_GROUP_NAME,
    TargetGroupProperties,
} from './api'
import {
    OnEventRequest,
    OnEventResponse,
} from '@aws-cdk/custom-resources/lib/provider-framework/types'
import { ELBv2 } from 'aws-sdk'

// CFn Docs: https://docs.aws.amazon.com/ja_jp/AWSCloudFormation/latest/UserGuide/aws-resource-elasticloadbalancingv2-targetgroup.html

const elbv2 = new ELBv2()

const createTargetGroup = async (
    event: OnEventRequest
): Promise<OnEventResponse> => {
    const {
        Name,
        Port,
        Protocol,
        ProtocolVersion,
        VpcId,
        TargetType,
        HealthCheckProtocol,
        HealthCheckPath,
        Matcher,
        HealthCheckIntervalSeconds,
        HealthCheckPort,
        UnhealthyThresholdCount,
        HealthyThresholdCount,
        HealthCheckTimeoutSeconds,
        HealthCheckEnabled,
    } = event.ResourceProperties as TargetGroupProperties

    if (!Name) {
        throw new Error(`Name is required`)
    }
    if (!Port) {
        throw new Error(`Port is required`)
    }
    if (!Protocol) {
        throw new Error(`Protocol is required`)
    }
    if (!ProtocolVersion) {
        throw new Error(`ProtocolVersion is required`)
    }
    if (!VpcId) {
        throw new Error(`VpcId is required`)
    }
    if (!TargetType) {
        throw new Error(`TargetType is required`)
    }

    const tg = await elbv2
        .createTargetGroup({
            Name,
            Port,
            Protocol,
            ProtocolVersion,
            VpcId,
            TargetType,
            HealthCheckEnabled: HealthCheckEnabled === 'true',
            HealthCheckIntervalSeconds,
            HealthCheckPath,
            HealthCheckPort,
            HealthCheckProtocol,
            HealthCheckTimeoutSeconds,
            HealthyThresholdCount,
            Matcher,
            UnhealthyThresholdCount,
        })
        .promise()
    const params = {
        Attributes: [
            {
                Key: "deregistration_delay.timeout_seconds",
                Value: "30"
            }
        ],
        TargetGroupArn: tg.TargetGroups![0].TargetGroupArn!
    }
    await elbv2.modifyTargetGroupAttributes(params).promise()

    console.log('create response: ', tg.TargetGroups![0])

    return {
        PhysicalResourceId: tg.TargetGroups![0].TargetGroupArn!,
        Data: {
            [ATTR_LOAD_BALANCER_ARNS]: tg.TargetGroups![0].LoadBalancerArns,
            [ATTR_TARGET_GROUP_NAME]: tg.TargetGroups![0].TargetGroupName,
            [ATTR_TARGET_GROUP_FULL_NAME]: tg.TargetGroups![0].TargetGroupArn!.split(
                ':'
            )[5],
        },
    }
}

const deleteTargetGroup = async (
    event: OnEventRequest
): Promise<OnEventResponse> => {
    await elbv2
        .deleteTargetGroup({ TargetGroupArn: event.PhysicalResourceId! })
        .promise()

    return {}
}
const updateTargetGroup = async (
    event: OnEventRequest
): Promise<OnEventResponse> => {
    if (!event.PhysicalResourceId) {
        throw new Error('PhysicalResourceId(TargetGroupArn) is required')
    }

    const {
        HealthCheckEnabled,
        HealthCheckIntervalSeconds,
        HealthCheckPath,
        HealthCheckPort,
        HealthCheckProtocol,
        HealthCheckTimeoutSeconds,
        HealthyThresholdCount,
        Matcher,
        UnhealthyThresholdCount,
    } = event.ResourceProperties as TargetGroupProperties

    const tg = await elbv2
        .modifyTargetGroup({
            TargetGroupArn: event.PhysicalResourceId,
            HealthCheckEnabled: HealthCheckEnabled === 'true',
            HealthCheckIntervalSeconds,
            HealthCheckPath,
            HealthCheckPort,
            HealthCheckProtocol,
            HealthCheckTimeoutSeconds,
            HealthyThresholdCount,
            Matcher,
            UnhealthyThresholdCount,
        })
        .promise()

    console.log('update response: ', tg.TargetGroups![0])

    return {
        PhysicalResourceId: tg.TargetGroups![0].TargetGroupArn,
        Data: {
            [ATTR_LOAD_BALANCER_ARNS]: tg.TargetGroups![0].LoadBalancerArns,
            [ATTR_TARGET_GROUP_NAME]: tg.TargetGroups![0].TargetGroupName,
            [ATTR_TARGET_GROUP_FULL_NAME]: tg.TargetGroups![0].TargetGroupArn!.split(
                ':'
            )[5],
        },
    }
}

export const handler = async (
    event: OnEventRequest
): Promise<OnEventResponse> => {
    console.log('event: ', event)

    switch (event.RequestType) {
        case 'Create': {
            return createTargetGroup(event)
        }
        case 'Update': {
            const targetGroupProps = event.ResourceProperties as TargetGroupProperties
            const oldTargetGroupProps = event.OldResourceProperties as TargetGroupProperties

            if (targetGroupProps.Name !== oldTargetGroupProps.Name) {
                await deleteTargetGroup(event)
                return createTargetGroup(event)
            }
            if (targetGroupProps.Port !== oldTargetGroupProps.Port) {
                await deleteTargetGroup(event)
                return createTargetGroup(event)
            }
            if (targetGroupProps.Protocol !== oldTargetGroupProps.Protocol) {
                await deleteTargetGroup(event)
                return createTargetGroup(event)
            }
            if (
                targetGroupProps.ProtocolVersion !== oldTargetGroupProps.ProtocolVersion
            ) {
                await deleteTargetGroup(event)
                return createTargetGroup(event)
            }
            if (targetGroupProps.TargetType !== oldTargetGroupProps.TargetType) {
                await deleteTargetGroup(event)
                return createTargetGroup(event)
            }
            if (targetGroupProps.VpcId !== oldTargetGroupProps.VpcId) {
                await deleteTargetGroup(event)
                return createTargetGroup(event)
            }

            return updateTargetGroup(event)
        }
        case 'Delete': {
            return deleteTargetGroup(event)
        }
    }
}
