# Demo Repo for .NET Runtime Bug #1029

This is a repository with test cases for reproducing the .NET runtime bug #1029,
where XMLReader can hang if its reading a single-element document from an open
stream.

The repo includes four test cases:

  - Cases with the stream opened and closed
  - Cases where the element is read via `reader.Value` and
    `reader.ReadElementContentAsString()`.

The test cases are written in xUnit - you can run the tests via `dotnet test`.