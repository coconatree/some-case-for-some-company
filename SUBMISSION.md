# Submission Notes

Unfortunately, I did not maintain a commit history for this exercise and by the time I realized it, it was too late for it to be useful. Therefore, I've listed the major changes I made below.

The project was developed using .NET 6. It has been successfully compiled and tested on my Windows machine.

The Postman collection I used for testing and interacting with the API is also included in the submission files.

## Changes on the "Program.cs"

- Registered the `InMemoryRobotNotifier`.
- Added in-memory cache.

## Changes to the Controllers

- Moved request objects into their own files.
- Moved business logic and object mapping responsibilities to the service layer.
- Removed the Service Locator anti-pattern.

## Updates to the Data Service

- Replaced manually concatenated SQL queries with parameterized queries.
- Added dedicated mapper functions for converting database results into objects.
- Added a milking record processing function (`ProcessMilkingRequest`) with animal record locking.

## Updates to the Notification Service

- Added the `Microsoft.Extensions.Caching.Memory` package to use in-memory caching.
- Implemented a helper method for executing actions with retry logic.
- Completed the implementation.

## Changes & New Tests

- Replaced `GetNextAnimalId` with `CreateIdentificationNumber` and GUIDs, since the value was used as an identification number and did not need to be an integer.
- Added new test files:
  - `MilkingsControllerTests.cs`
  - `WeightsControllerTests.cs`
  - `NotificationServiceUnitTests.cs`

### Notes About Tests

The controller tests cover both positive and negative scenarios. Ideally, an additional test case would verify behavior when a robot exists but is not active.

Under `MilkingsControllerTests.cs`, there is a test validating that asynchronous calls to `RecordMilking` only allow one execution to go through. More scenarios can be tested.

The notification service tests are unit tests that verify subscriber execution and check whether disposal behaves correctly. Additional test coverage could be added, particularly around caching behavior and subscriber execution scenarios.

## If I Had More Time and Authority, I Would...

- Standardize error messages across the application.
- Implement application-level exception handling.
- Write end-to-end (E2E) tests.
- Extend the test suite using a mocking library.
- Introduce an in-memory database for testing.
- Create a dedicated service/repository layers and separate responsibilities.
- Add request interceptors and move validation logic out of the controllers.
- Migrate the notification interface to use `Func<T, Task>` and leverage `Task.WhenAll()` to execute subscribers asynchronously, rather than iterating through them on a separate thread.
- Refactor service-layer methods for better reusability (e.g., `GetAllAnimals(page, size, sortBy, filterBy)`).
- Introduce DTOs and use a mapping library to automate object mappings.
- Return DTOs instead of anonymous ID objects or internal database models.
- Move the database migration that inserts seed data into test-specific database scripts so that it does not affect environments.
- Add/extend test data helper/fixture and remove duplicated test setup code.
- Handle duplicate key and SQL exceptions.
- Add a `Dockerfile` and integrate the service into the existing `docker-compose` configuration.
- Upgrade the .NET version to at least .NET 8.

### About Exception Handling

Currently, there are:
- Exceptions coming from data annotation validations.
- Exceptions coming from manually catching and returning `BadRequest()` (and similar responses).
- Exceptions that can occur but are not currently handled (internal server exceptions).

These should be standardized and handled through an application-level exception handler.

Also, manually defined error messages should be moved to static constants so they can be reused in tests without hardcoding.

### Package Compatibility

There are package compatibility warnings related to the project's .NET version. The project should be upgraded to at least .NET 8 to address them. Due to time constraints, I didn't do it.

### Possible Null Dereferences

I reviewed most of the nullable dereference warnings reported by the compiler. In the cases I investigated, the warnings appeared to be false positives.

## Hindsight is 20/20

There is a tight coupling between the `data service` and the `notification service`. I am not satisfied with this. 

Additionally, there is an edge-case race condition in which a delayed cache update may cause a subsequent transaction to read stale data.

## Decisions

- I added an in-memory cache to provide fast responses for recently milked queries. I assumed a single-instance deployment for this exercise, where in-memory caching is valid.

- I used a database-level lock to prevent concurrent milking operations on the same animal.

- I placed the notification step after the database commit to ensure that cache updates only occur for successfully persisted transactions.

- I passed `IRobotNotifier` as a method-level dependency in the milking processing flow to avoid a circular dependency between the data service and notification service. This is a temporary tradeoff due to time constraints.