# dappi - Dotnet API Pre-Programming Interface

dappi is a powerful tool designed to streamline backend API development by automatically generating controllers with CRUD endpoints for a given entity.

## Features

- **Automatic Controller Generation:** Controllers are automatically generated for CRUD operations and they offer:
    - Filtering by any field
    - Pagination
    - Sorting

## Installation

1. Create a .NET API Project, add nuget package

    ```dotnet add package Codechem.Dappi.Generator --version 1.0.0```

## Usage

### 1. Create a Model

To create a model, define a class and apply the `[CCController]` attribute to it. This will automatically generate a controller for the model.

Example:

```csharp
[CCController]
public class Book
{
    public string Name { get; set; }
    public string Surname { get; set; }
}

After saving the file with any IDE, you run the app, and you should be able to access your CRUD endpoints section under /api/book/ url.
