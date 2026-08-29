---
title: Messaging and events
description: Publishing events reliably, the outbox pattern, and designing consumers that tolerate reality.
order: 40
---

The moment a second service needs to know something happened, you have a distributed system. The patterns below exist to keep it honest.

## Commands and events

- A **command** tells one service to do something (`ChargePayment`). It has one handler and can be rejected.
- An **event** states that something happened (`OrderPlaced`). It has any number of subscribers and cannot be rejected.

Name events in the past tense, and design them so the publisher does not care who listens.

## The dual-write problem

```csharp
await db.SaveChangesAsync(ct);                 // committed
await bus.PublishAsync(new OrderPlaced(...));  // process dies here → event lost forever
```

Two systems, no shared transaction. The fix is the **outbox**: write the event to the same database, in the same transaction, and publish it afterwards.

```csharp
db.Orders.Add(order);
db.OutboxMessages.Add(OutboxMessage.For(new OrderPlaced(order.Id, order.Total)));
await db.SaveChangesAsync(ct);                 // both, or neither
```

A [background service](/docs/ops/background-services) polls the outbox, publishes, and marks rows sent. That gives **at-least-once** delivery — which is the strongest guarantee worth paying for.

## Therefore: idempotent consumers

At-least-once means duplicates. Every consumer must tolerate them:

```csharp
if (await db.ProcessedMessages.AnyAsync(m => m.Id == message.Id, ct)) return;

await HandleAsync(message, ct);
db.ProcessedMessages.Add(new ProcessedMessage(message.Id, time.GetUtcNow()));
await db.SaveChangesAsync(ct);
```

Or make the operation naturally idempotent (`SET status = 'shipped'` rather than `increment`).

## Ordering

Most brokers guarantee order only within a partition or session. If two events for the same order must be processed in sequence, partition by order id. If they must not be reordered across entities, you are asking the broker for something it does not provide — put a version on the event and let the consumer discard stale ones.

## Poison messages

A message that always fails will retry forever and block the partition. Bound retries, then dead-letter it — with an alert. An unwatched dead-letter queue is silent data loss.

## Schema evolution

Producers and consumers deploy independently, so both versions run at once. Only add optional fields; never rename or repurpose one. Version the event type when you must break it (`OrderPlacedV2`) and publish both for a transition period.

## Do you need a broker?

For a single service, an in-process bus or a bounded [`Channel<T>`](/docs/runtime/threading) is honest and simple. For work that must survive a restart, or that crosses a service boundary, use a real broker with an outbox. MassTransit and NServiceBus provide outbox, retry, and dead-letter handling over Azure Service Bus, RabbitMQ, SQS, and Kafka — worth using rather than rebuilding.

## Further reading

- [Asynchronous message-based communication](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/asynchronous-message-based-communication)
