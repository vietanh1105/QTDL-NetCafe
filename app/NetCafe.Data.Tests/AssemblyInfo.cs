// Trách nhiệm: chạy integration test tuần tự để fixture test_xunit_* không tranh chấp máy.

using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
