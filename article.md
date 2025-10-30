# Building a Distributed Sequence Generator with ZooKeeper and C\#


![logo](img/logo.png "Logo")

## 1. Goal and Motivation

This demo project explores how to assign unique, dynamic sequence numbers to multiple server instances in a distributed environment, inspired by challenges that surface when designing things like global ID generators (e.g., Twitter’s Snowflake). As described in the project’s README, the scenario began as a typical interview problem: how do you ensure each server instance receives (and keeps) a unique ID, especially if instances are started, stopped, or restarted?

ZooKeeper is leveraged here to coordinate between all node servers, and to manage these lifecycle and uniqueness challenges in a robust, automatic way.

## References:
- <img src="https://img.shields.io/badge/-181717?style=flat&logo=github&logoColor=white" alt="GitHub" /> [Demo Github Repo](https://github.com/rulyotano/example-sequence-server-zookeeper)
- [Nice Medium post that helped me to understand what ZooKeeper is](https://bikas-katwal.medium.com/zookeeper-introduction-designing-a-distributed-system-using-zookeeper-and-java-7f1b108e236e)
- [Nice Zookeeper guide](https://www.tutorialspoint.com/zookeeper/zookeeper_quick_guide.htm)
- [ZooKeeper Docker Image](https://hub.docker.com/_/zookeeper)
- [ZooKeeper Dotnet Library](https://github.com/shayhatsor/zookeeper)


## 2. What is ZooKeeper, and How Does It Help?

**ZooKeeper** is a distributed coordination service. It can be used to manage configuration, naming, synchronization, and group services for large distributed systems. ZooKeeper organizes its data in a file-system-like hierarchy called "znodes." These znodes can store both data and metadata, and are kept in sync across the ZooKeeper ensemble.

### Key features

- **Ephemeral znodes:** These are znodes that exist as long as the client that created them maintains its connection—perfect for live service registration.
- **Watcher mechanism:** Clients can set watches to be notified about specific changes (e.g., a znode disappearing, data changing, or children being added or removed). This enables real-time ("live") updates and coordination between services.

**In this demo:**
- Each server instance tries to claim an ID by creating a numbered ephemeral znode under a parent `/sequence` node.
- If an instance dies or disconnects, its znode is automatically deleted, freeing up its number for future instances.
- New instances dynamically see available ("free") numbers and claim the lowest unused one.

## 3. Using ZooKeeper znodes to Track Instance IDs (Walkthrough of the Demo)

The core logic of assigning and tracking instance sequence numbers lives in the [`ZookeeperDistributedConfiguration`](code/SequenceNode/Infrastructure/Zoo/ZookeeperDistributedConfiguration.cs)  class.

```
<script src="https://gist.github.com/rulyotano/d03cfb7be4f7c59b40685a0a458be020.js"></script>
```



### Main Elements of the Implementation

- **Sequence Node Initialization:**  
  The parent `/sequence` znode is created if it doesn't exist.
- **Assigning IDs:**  
  Each server, on startup, looks for the lowest available number (by checking the children of `/sequence`), and tries to create an ephemeral znode like `/sequence/1`, `/sequence/2`, etc.
- **Ephemeral znodes:**  
  The use of `CreateMode.EPHEMERAL` ensures znodes are removed if a server disconnects, making numbers immediately reusable.
- **Live Coordination:**  
  When servers join or leave, the list of children under `/sequence` is updated live, so every instance knows which IDs are in use and which are free.
- **Connection Handling:**  
  Robust connection and reconnection logic (with retries and watcher callbacks) is implemented using ZooKeeper’s watcher/event system.

#### Example: ID Allocation Logic

```csharp
private async Task AssignSequenceIfNoAsync(CancellationToken cancellationToken)
{
  await InitializeAsync();
  if (IsSequenceAssigned()) return;
  var created = false;
  var triesLeft = 5;

  while (!created && triesLeft > 0)
  {
    var assignedSequenceNumbers = await GetAssignedSequenceNumbersAsync(cancellationToken);
    if (assignedSequenceNumbers.Count == 0 || assignedSequenceNumbers[0] != FirstSequence)
    {
      _sequenceNumber = FirstSequence;
    }
    _sequenceNumber = FindFirstFreeSequenceNumber(assignedSequenceNumbers);

    created = await AssignSequenceNumberAsync(_sequenceNumber, cancellationToken);
    triesLeft--;
  }
}
```

### Summary of C\# Implementation

- **Minimalist and Direct:**  
  The approach is intentionally simple, focusing on basic ZooKeeper primitives rather than advanced recipes or third-party libraries.
- **Classes of Interest:**  
  - `ZookeeperDistributedConfiguration`: Main logic for sequence assignment.
  - `ZookeeperConnection`: Handles the ZooKeeper client, connection retries, and reacting to ZooKeeper events.
  - `ZookeeperWatcher`: Implements event/subscription logic to handle live updates triggered by ZooKeeper state changes.

---

This demo exemplifies a hands-on, minimal setup for learning about ZooKeeper, distributed coordination, and ephemeral resource assignment using znodes. It also illustrates how even a basic C# implementation can make use of these powerful coordination patterns.

## 4. Practical Demo: Running & Testing the Distributed Sequence Server

This section walks you through running and testing the distributed sequence number server using Docker and Docker Compose. The scenario is based directly on the steps and demos from the project README.

### Prerequisites
- Docker and Docker Compose installed.

### Step 1: Start ZooKeeper

```bash
cd ./code
docker compose up -d zookeeper
```
Now you have a simple ZooKeeper instance running.

#### (Optional) Test the ZooKeeper Instance
```bash
docker ps # or docker container ls
# Find the zookeeper container ID, then
# Connect to ZooKeeper CLI:
docker exec -it <container-id> zkCli.sh
# In ZooKeeper CLI, try:
ls /
```

Should see:

```
[zk: localhost:2181(CONNECTED) 1] ls /
[zookeeper]
```

### Step 2: Build and Run SequenceNode Instances

Build the SequenceNode Docker image:
```bash
cd ./code/SequenceNode
docker image build -t sequencenode --target prod .
```

Start multiple SequenceNode containers:
```bash
docker run -p 5001:80 --name sequencenode1 --network code_default -d sequencenode && \
docker run -p 5002:80 --name sequencenode2 --network code_default -d sequencenode && \
docker run -p 5003:80 --name sequencenode3 --network code_default -d sequencenode && \
docker run -p 5004:80 --name sequencenode4 --network code_default -d sequencenode
```
Note: The `code_default` network is created by compose by default. If different, adjust the `--network` flag accordingly.

### Step 3: Testing
- Open a terminal or browser to test the sequence API.

#### Using curl:
```bash
curl http://localhost:500x/sequence
```
Replace `x` with 1, 2, 3, or 4, depending on which container you want to test.

We should get:

```
% curl http://localhost:5002/sequence                                                                                                    ~
2%
```

#### Using a browser:
Visit:
- http://localhost:500x/swagger (API docs)
- http://localhost:500x/sequence (direct endpoint)

#### Demo Sequence
1. Query sequence for 1, 2, and 3.
2. Stop 2: `docker container stop sequencenode2`
3. Query for 4 (should now claim the freed number 2!)
4. Restart 2: `docker container start sequencenode2`
5. Query 2 again, now should assign sequence 4.

#### Notes on ZooKeeper Container
If you stop the ZooKeeper container, all sequence API requests will fail. When restarted, requests work again, but ephemeral znodes may not be deleted; new sequences will start after the last inserted (e.g., 5 in the running example).

### Step 4: Test with Docker Compose Scaling

1. Change the number of replicas in `docker-compose.yml`, e.g. from 1 to 4.
2. Run:
```bash
docker compose up -d
```
3. All replicas start up. Access via Docker networking:

Connect directly to containers:
  
```bash
docker ps # Find container ID
docker exec <container-id> wget -qO- http://localhost/sequence
```

Or create an Alpine container attached to the same network:
```bash
docker run -it --rm --network code_default alpine sh
wget -qO- http://node/sequence
```

This approach lets you observe ephemeral sequence allocation under load balancing and real distributed conditions.

---

By following the steps above, you can reproduce the entire demo setup and observe dynamic, ephemeral sequence assignments in a real distributed system using ZooKeeper and Docker.