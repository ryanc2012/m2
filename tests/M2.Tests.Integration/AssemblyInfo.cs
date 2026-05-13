// Disable parallel execution across test classes in this assembly.
// All integration tests use WebApplicationFactory<Program>, which runs Program.cs
// top-level statements (including static Serilog bootstrap) per factory instance.
// Parallel class execution causes a race on Log.Logger static assignment, leading to
// "entry point exited without ever building an IHost" failures.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
