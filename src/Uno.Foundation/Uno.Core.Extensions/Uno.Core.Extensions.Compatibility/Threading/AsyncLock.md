# Async Lock

## Concept
The idea of the `AsyncLock` is to provide an asynchronous equivalent of the `lock` keyword, in order to protect a ressource while working asynchronously.

## Usage
```csharp
1: using (await gate.LockAsync(ct))
2: {
3:	// Safe section
4:	await something;
5: }
6: // Continuation
```

## FastAsyncLock
The `FastAsyncLock` has the same contract of the `AsyncLock` but it differs on two points:

1. It allows re-entrency
1. When releasing the lock, next awaiters are run synchronously

While the first point is pretty obvious, the second point is more tricky. Lets assume the example: *Task #1* acquired the lock (line 3 in example above), while *Task #2* is waiting for it (line 1).

With the `AsyncLock` when the thread that is running *Task #1* releases the lock, it will schedule on the `TaskScheduler` the continuation of the *Task #2* while it continues to execute *Task #1*.

With the `FastAsyncLock` will instead continue *Task #2* (i.e. entering in the safe section of the *Task #2*) **before** continuing the *Task #1*


### `AsyncLock`

|  Main thread		| *any available thread of task pool* | *any available thread of task pool* |
| ----------------- | ----------------- |  -------------------- |
| line 3 of task #1 |					| 						|
| line 4 of task #1 |					| 						|
| line 5 of task #1 |					| 						|
| line 6 of task #1 | line 2 of task #2 | 						|
|					| line 3 of task #2 | 						|
|					| line 4 of task #2 | 						|
|					| 					| something of task #2	|


### `FastAsyncLock`

|  Main thread		| *any available thread of task pool* |
| ----------------- | --------------------- |
| line 3 of task #1 |						|
| line 4 of task #1 |						|
| line 5 of task #1 |						|
| line 2 of task #2 |						|
| line 3 of task #2 |						|
| line 4 of task #2 |						|
| line 6 of task #1 | something	of task #2	|