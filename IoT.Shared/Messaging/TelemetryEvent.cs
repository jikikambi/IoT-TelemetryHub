namespace IoT.Shared.Messaging;

public record TelemetryEvent(string MessageId, DateTimeOffset OccurredAt, string DeviceId, string Type, string PayloadJson);
