# ❄️ SnowFlake  
High-performance, distributed, and collision-free unique ID generator.

---

## Overview

SnowFlake is a modern, low-latency unique ID generator built for distributed systems that require strict chronological ordering, high throughput, and zero collisions. Inspired by Twitter's original Snowflake algorithm, this implementation is written from scratch in C# and designed to scale across datacenters, threads, and cores.

It’s suitable for production systems like messaging platforms, social networks, distributed databases, microservices, and anywhere else you need millions of unique IDs per second without central coordination.

---

## Features

- 🔹 64-bit integer IDs
- 🔹 Strictly ordered (time-based)
- 🔹 Thread-safe, lock-free core
- 🔹 Supports millions of IDs/sec
- 🔹 Datacenter and Worker ID separation
- 🔹 Custom epoch support
- 🔹 No external dependencies
- 🔹 Suitable for proprietary or commercial systems

---

## ID Structure (64-bit)

| Bits  | Field         | Description                      |
|-------|---------------|----------------------------------|
| 1     | Unused        | Reserved                         |
| 42    | Timestamp     | Time since custom epoch (µs/ms)  |
| 8     | Datacenter ID | Logical data center (0–255)      |
| 8     | Worker ID     | Instance/thread/machine ID       |
| 5-13  | Sequence      | Counter for same-timestamp IDs   |

> This layout supports large clusters with multiple datacenters and high-throughput generators per node.

---

## Basic Usage

```csharp
var generator = new SnowflakeCoreGenerator(datacenterId: 1, workerId: 10);
long id = generator.NextId();
