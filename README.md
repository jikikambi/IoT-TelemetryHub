# `README.md` for PulseNet.Microservices

# PulseNet.Microservices - IoT Platform (.NET 9, gRPC + REST + RabbitMQ)

⚠️ **Warning – Work in Progress**  
This project is **under active development**. Features, APIs, and project structure may change. Some functionality is only partially implemented. Please consider the current implementation **not final**.

## Overview

PulseNet.Microservices is a modular IoT framework built on .NET 8. It implements device telemetry ingestion, command processing, and secure JWT-based authentication.  
It extends the IoT Starter blueprint with deterministic testing capabilities, DeviceWorkers, and end-to-end telemetry/command flows.

## Architecture Overview

```
    ┌──────────────┐
    │ DeviceWorker │
    │ (IoT.DeviceApp)│
    └──────┬───────┘
   Telemetry │   ▲ Commands
             │   │
             ▼   │
    ┌──────────────┐
    │ IoT.Gateway  │
    │  gRPC Server │
    └──────┬───────┘
           │ Publish Telemetry
           ▼
    ┌──────────────┐
    │  RabbitMQ    │
    └──────┬───────┘
   Telemetry │ Commands
             ▼
    ┌──────────────┐
    │TelemetryIngestor│
    │ (Background Worker) │
    └──────────────┘

    ┌──────────────┐
    │  IoT.DeviceApi │
    │   REST API     │
    └──────┘
      │ Sends Commands
      ▼
    ┌──────────────┐
    │  RabbitMQ    │
    └──────────────┘
```

```

- Telemetry: DeviceWorker → Gateway → RabbitMQ → TelemetryIngestor  
- Commands: DeviceApi → RabbitMQ → DeviceWorker  
- `IoT.Shared` contains shared DTOs, messaging helpers, and gRPC contracts.

---

## Microservice Interaction Table

| Service | Interacts With | Notes |
|---------|----------------|-------|
| DeviceWorker (IoT.DeviceApp) | IoT.Gateway, RabbitMQ | Streams telemetry and receives commands |
| IoT.Gateway | RabbitMQ | Publishes telemetry for ingestion |
| IoT.DeviceApi | RabbitMQ | Sends commands to devices |
| TelemetryIngestor | RabbitMQ, IoT.Telemetry.Persistence | Consumes telemetry messages and persists/logs them |
| IoT.Shared | All services | Contains shared DTOs, messaging helpers, and gRPC contracts |
| IoT.Telemetry.Persistence | TelemetryIngestor | Optional database persistence layer |

---

## Solution Structure

```

PulseNet.Microservices.sln
│
├── IoT.Contracts                  # Shared contracts and DTOs used across services (✅ Implemented)
├── IoT.DeviceApi                  # REST API to send commands to devices (⚠️ Partially implemented)
├── IoT.DeviceApp                  # DeviceWorker: streams telemetry, receives commands, manages JWT (✅ Implemented)
├── IoT.DeviceApp.UnitTests        # Unit tests for DeviceWorker, JWT refresh, telemetry/command handling (✅ Implemented)
├── IoT.DeviceApp.IntegrationTests # Integration tests with FakeDeviceGatewayClient for deterministic behavior (✅ Implemented)
├── IoT.Gateway                     # gRPC Device Gateway: receives telemetry from devices and publishes to messaging (⚠️ Partially implemented)
├── IoT.Shared                      # Shared libraries: messaging helpers, DTOs, proto-generated classes (✅ Implemented)
├── IoT.Shared.UnitTests            # Unit tests for shared libraries (✅ Implemented)
├── IoT.Telemetry.Persistence       # Database persistence for telemetry (⚠️ Partially implemented)
└── IoT.TelemetryIngestor           # Background worker consuming telemetry messages and logging them (⚠️ Partially implemented)

````

---

## Work Implemented So Far (with Status)

### 1. **IoT.DeviceApp** ✅ Implemented
- `DeviceWorker` connects to `IDeviceGatewayClient`.
- Streams telemetry to the gRPC gateway.
- Receives commands asynchronously.
- JWT token handling with automatic refresh.
- Deterministic integration and unit testing with fakes.

### 2. **IoT.DeviceApp.UnitTests** ✅ Implemented
- Tests for JWT refresh, telemetry ack capture, and command processing.
- Uses **FakeItEasy** for mocking `IDeviceGatewayClient`.

### 3. **IoT.DeviceApp.IntegrationTests** ✅ Implemented
- End-to-end tests using `FakeDeviceGatewayClient`.
- Validates telemetry streaming, ack generation, and command reception.

### 4. **IoT.Gateway** ⚠️ Partially Implemented
- gRPC server to receive telemetry from devices.
- Publishes telemetry to RabbitMQ (basic implementation in place, more features planned).

### 5. **IoT.DeviceApi** ⚠️ Partially Implemented
- REST API endpoint to send commands to devices.
- Commands published to RabbitMQ.
- Full API routes and validation not complete yet.

### 6. **IoT.Shared** ✅ Implemented
- Shared DTOs, messaging helpers, proto-generated gRPC contracts.
- Common interfaces used by workers and tests.

### 7. **IoT.Shared.UnitTests** ✅ Implemented
- Unit tests for shared DTOs and messaging helpers.

### 8. **IoT.TelemetryIngestor** ⚠️ Partially Implemented
- Background worker consuming telemetry from RabbitMQ.
- Logging working; persistence to DB not fully wired yet.

### 9. **IoT.Telemetry.Persistence** ⚠️ Partially Implemented
- Database layer for telemetry.
- Schema exists but full CRUD integration with `TelemetryIngestor` is pending.

### 10. **IoT.Contracts** ✅ Implemented
- Shared contracts and DTOs for consistent messaging and telemetry schemas.

---

## Features

| Feature | Status |
|---------|--------|
| Device telemetry ingestion via gRPC | ✅ Fully implemented |
| Command delivery via REST + RabbitMQ | ⚠️ Partially implemented |
| JWT authentication with automatic refresh | ✅ Fully implemented |
| Deterministic unit and integration testing | ✅ Fully implemented |
| Telemetry persistence and logging | ⚠️ Partially implemented |
| Modular microservice architecture | ✅ Implemented |

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker (for RabbitMQ)
- IDE: Visual Studio / Rider / VSCode

### Build & Run

```bash
# Clone repo
git clone https://github.com/your-org/PulseNet.Microservices.git
cd PulseNet.Microservices

# Restore and build
dotnet restore
dotnet build
````

### Running with Docker

```bash
docker compose up --build
```

* Starts RabbitMQ and supporting services.
* Use `tools/simulate-device.sh` to simulate a device streaming telemetry.

---

## Developer Quickstart (Local Fake DeviceWorker)

Run **DeviceWorker locally using fakes only**, without RabbitMQ or a real gRPC gateway.

```csharp
using IoT.DeviceApp;
using IoT.Shared;
using IoT.Shared.Fakes;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var fakeGateway = new FakeDeviceGatewayClient();
        var worker = new DeviceWorker(fakeGateway);

        worker.OnTelemetryReceived += (telemetry) =>
        {
            Console.WriteLine($"Telemetry sent: {telemetry.DeviceId} -> {telemetry.Value}");
        };

        worker.OnCommandReceived += (command) =>
        {
            Console.WriteLine($"Command received: {command.CommandName}");
        };

        await worker.StartAsync();

        Console.WriteLine("DeviceWorker running locally with fakes. Press any key to stop.");
        Console.ReadKey();

        await worker.StopAsync();
    }
}
```

*This allows developers to:*

* Simulate telemetry streaming.
* Receive fake commands.
* Observe telemetry acks in real-time.
* Test JWT refresh logic deterministically.

---

## Testing

* **Unit Tests**:

```bash
dotnet test ./tests/IoT.DeviceApp.UnitTests
dotnet test ./tests/IoT.Shared.UnitTests
```

* **Integration Tests**:

```bash
dotnet test ./tests/IoT.DeviceApp.IntegrationTests
```

* Tests cover:

  * JWT token refresh.
  * Telemetry acks.
  * Command processing.

---

## Future Roadmap

| Feature / Improvement              | Priority | Notes                                                                                        |
| ---------------------------------- | -------- | -------------------------------------------------------------------------------------------- |
| Full REST API completion           | High     | Complete all routes, validation, and error handling for `IoT.DeviceApi`                      |
| Advanced command scheduling        | Medium   | Add scheduling and retry logic for device commands                                           |
| Telemetry persistence enhancements | High     | Full CRUD operations in `IoT.Telemetry.Persistence` and integration with `TelemetryIngestor` |
| Device group management            | Medium   | Support multiple devices per group with shared commands and telemetry streams                |
| Gateway enhancements               | Medium   | Add load balancing, authentication, and metrics to gRPC gateway                              |
| Monitoring & logging dashboard     | Low      | Visualize telemetry streams, commands, and JWT expirations                                   |
| CI/CD pipeline                     | High     | Automate builds, tests, and deployment                                                       |

---

## Contributing

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/my-feature`.
3. Implement changes and write tests.
4. Run `dotnet test` to verify all tests pass.
5. Submit a pull request.

---

## License

MIT License. See [LICENSE](LICENSE) for details.

---

## Notes

* ⚠️ The project is **still in progress**. Expect API, gateway, and persistence layers to evolve.
* Fully implemented components (✅) are safe to use in tests or simulations.
* Partially implemented components (⚠️) are experimental; breaking changes may occur.
* Developer Quickstart allows running DeviceWorker locally with fakes for rapid testing without dependencies.
* ASCII diagram provides a high-level overview of telemetry and command flow across microservices.
* Microservice Interaction Table shows which service depends on which other services.
* Future Roadmap gives contributors visibility into upcoming features and priorities.
