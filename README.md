# dappi - Dotnet API Pre-Programming Interface

dappi is a powerful tool designed to streamline backend API development by automatically generating controllers with CRUD endpoints for a given entity.

## Features

- **Out of the box headless CMS:** The `Dappi.HeadlessCms` library combined with our template project will:
    - Turn your API into a headless CMS similar to Strapi.
    - Allow content type and content management via an Admin panel.
    - Generate backend code that you can use for version control and easy deployments.

- Dappi by default uses a source generator for CRUD controllers which is exposed through the `Dappi.SourceGenerator` library which offers:
    - **Automatic Controller Generation**:
        - Filtering by any field
        - Pagination
        - Sorting

     
## Getting Started

### Prerequisites

You should have [.NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)  installed. 

### Installation

1. Install the Dappi CLI.
```sh
  dotnet tool install --global Dappi.Cli --version 0.1.5-preview
```
2. Initialize your project
 ```sh
    dappi init --name <PROJECT-NAME> --path <OUTPUT-DIRECTORY> --use-prerelease
 ```
3. Modify the connection string to point to your database in `appsettings.json`
```json
 ...
  "Dappi":  {  
     "PostgresConnection":"YOUR-CONNECTION-STRING"
  },
  ...
```
4. Then you're ready to start your project. Navigate to your project's directory and run
```sh
    dappi start
```
