﻿// <copyright company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// <auto-generated/>

#nullable enable

using Datadog.Trace.Processors;
using Datadog.Trace.Tagging;
using System;

namespace Datadog.Trace.Tagging
{
    partial class KafkaTags
    {
        // MessageQueueTimeMsBytes = MessagePack.Serialize("message.queue_time_ms");
        private static ReadOnlySpan<byte> MessageQueueTimeMsBytes => new byte[] { 181, 109, 101, 115, 115, 97, 103, 101, 46, 113, 117, 101, 117, 101, 95, 116, 105, 109, 101, 95, 109, 115 };
        // SpanKindBytes = MessagePack.Serialize("span.kind");
        private static ReadOnlySpan<byte> SpanKindBytes => new byte[] { 169, 115, 112, 97, 110, 46, 107, 105, 110, 100 };
        // InstrumentationNameBytes = MessagePack.Serialize("component");
        private static ReadOnlySpan<byte> InstrumentationNameBytes => new byte[] { 169, 99, 111, 109, 112, 111, 110, 101, 110, 116 };
        // BootstrapServersBytes = MessagePack.Serialize("messaging.kafka.bootstrap.servers");
        private static ReadOnlySpan<byte> BootstrapServersBytes => new byte[] { 217, 33, 109, 101, 115, 115, 97, 103, 105, 110, 103, 46, 107, 97, 102, 107, 97, 46, 98, 111, 111, 116, 115, 116, 114, 97, 112, 46, 115, 101, 114, 118, 101, 114, 115 };
        // TopicBytes = MessagePack.Serialize("messaging.destination.name");
        private static ReadOnlySpan<byte> TopicBytes => new byte[] { 186, 109, 101, 115, 115, 97, 103, 105, 110, 103, 46, 100, 101, 115, 116, 105, 110, 97, 116, 105, 111, 110, 46, 110, 97, 109, 101 };
        // PartitionBytes = MessagePack.Serialize("kafka.partition");
        private static ReadOnlySpan<byte> PartitionBytes => new byte[] { 175, 107, 97, 102, 107, 97, 46, 112, 97, 114, 116, 105, 116, 105, 111, 110 };
        // OffsetBytes = MessagePack.Serialize("kafka.offset");
        private static ReadOnlySpan<byte> OffsetBytes => new byte[] { 172, 107, 97, 102, 107, 97, 46, 111, 102, 102, 115, 101, 116 };
        // TombstoneBytes = MessagePack.Serialize("kafka.tombstone");
        private static ReadOnlySpan<byte> TombstoneBytes => new byte[] { 175, 107, 97, 102, 107, 97, 46, 116, 111, 109, 98, 115, 116, 111, 110, 101 };
        // ConsumerGroupBytes = MessagePack.Serialize("kafka.group");
        private static ReadOnlySpan<byte> ConsumerGroupBytes => new byte[] { 171, 107, 97, 102, 107, 97, 46, 103, 114, 111, 117, 112 };

        public override string? GetTag(string key)
        {
            return key switch
            {
                "span.kind" => SpanKind,
                "component" => InstrumentationName,
                "messaging.kafka.bootstrap.servers" => BootstrapServers,
                "messaging.destination.name" => Topic,
                "kafka.partition" => Partition,
                "kafka.offset" => Offset,
                "kafka.tombstone" => Tombstone,
                "kafka.group" => ConsumerGroup,
                _ => base.GetTag(key),
            };
        }

        public override void SetTag(string key, string value)
        {
            switch(key)
            {
                case "messaging.kafka.bootstrap.servers": 
                    BootstrapServers = value;
                    break;
                case "messaging.destination.name": 
                    Topic = value;
                    break;
                case "kafka.partition": 
                    Partition = value;
                    break;
                case "kafka.offset": 
                    Offset = value;
                    break;
                case "kafka.tombstone": 
                    Tombstone = value;
                    break;
                case "kafka.group": 
                    ConsumerGroup = value;
                    break;
                case "span.kind": 
                case "component": 
                    Logger.Value.Warning("Attempted to set readonly tag {TagName} on {TagType}. Ignoring.", key, nameof(KafkaTags));
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        public override void EnumerateTags<TProcessor>(ref TProcessor processor)
        {
            if (SpanKind is not null)
            {
                processor.Process(new TagItem<string>("span.kind", SpanKind, SpanKindBytes));
            }

            if (InstrumentationName is not null)
            {
                processor.Process(new TagItem<string>("component", InstrumentationName, InstrumentationNameBytes));
            }

            if (BootstrapServers is not null)
            {
                processor.Process(new TagItem<string>("messaging.kafka.bootstrap.servers", BootstrapServers, BootstrapServersBytes));
            }

            if (Topic is not null)
            {
                processor.Process(new TagItem<string>("messaging.destination.name", Topic, TopicBytes));
            }

            if (Partition is not null)
            {
                processor.Process(new TagItem<string>("kafka.partition", Partition, PartitionBytes));
            }

            if (Offset is not null)
            {
                processor.Process(new TagItem<string>("kafka.offset", Offset, OffsetBytes));
            }

            if (Tombstone is not null)
            {
                processor.Process(new TagItem<string>("kafka.tombstone", Tombstone, TombstoneBytes));
            }

            if (ConsumerGroup is not null)
            {
                processor.Process(new TagItem<string>("kafka.group", ConsumerGroup, ConsumerGroupBytes));
            }

            base.EnumerateTags(ref processor);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (SpanKind is not null)
            {
                sb.Append("span.kind (tag):")
                  .Append(SpanKind)
                  .Append(',');
            }

            if (InstrumentationName is not null)
            {
                sb.Append("component (tag):")
                  .Append(InstrumentationName)
                  .Append(',');
            }

            if (BootstrapServers is not null)
            {
                sb.Append("messaging.kafka.bootstrap.servers (tag):")
                  .Append(BootstrapServers)
                  .Append(',');
            }

            if (Topic is not null)
            {
                sb.Append("messaging.destination.name (tag):")
                  .Append(Topic)
                  .Append(',');
            }

            if (Partition is not null)
            {
                sb.Append("kafka.partition (tag):")
                  .Append(Partition)
                  .Append(',');
            }

            if (Offset is not null)
            {
                sb.Append("kafka.offset (tag):")
                  .Append(Offset)
                  .Append(',');
            }

            if (Tombstone is not null)
            {
                sb.Append("kafka.tombstone (tag):")
                  .Append(Tombstone)
                  .Append(',');
            }

            if (ConsumerGroup is not null)
            {
                sb.Append("kafka.group (tag):")
                  .Append(ConsumerGroup)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
        public override double? GetMetric(string key)
        {
            return key switch
            {
                "message.queue_time_ms" => MessageQueueTimeMs,
                _ => base.GetMetric(key),
            };
        }

        public override void SetMetric(string key, double? value)
        {
            switch(key)
            {
                case "message.queue_time_ms": 
                    MessageQueueTimeMs = value;
                    break;
                default: 
                    base.SetMetric(key, value);
                    break;
            }
        }

        public override void EnumerateMetrics<TProcessor>(ref TProcessor processor)
        {
            if (MessageQueueTimeMs is not null)
            {
                processor.Process(new TagItem<double>("message.queue_time_ms", MessageQueueTimeMs.Value, MessageQueueTimeMsBytes));
            }

            base.EnumerateMetrics(ref processor);
        }

        protected override void WriteAdditionalMetrics(System.Text.StringBuilder sb)
        {
            if (MessageQueueTimeMs is not null)
            {
                sb.Append("message.queue_time_ms (metric):")
                  .Append(MessageQueueTimeMs.Value)
                  .Append(',');
            }

            base.WriteAdditionalMetrics(sb);
        }
    }
}
