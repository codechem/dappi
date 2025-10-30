using Dappi.Core.Utils;
using Dappi.HeadlessCms.Controllers;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Services;
using Dappi.HeadlessCms.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using Xunit.Extensions.Ordering;

namespace Dappi.HeadlessCms.Tests.Controllers
{
    [Collection("ModelsControllerTests"), Order(1)]
    public class ModelsControllerTests : BaseIntegrationTest, IDisposable
    {
        private readonly ModelsController _controller;
        private readonly string _entitiesPath;
        private readonly string _dbContextPath;

        private const string InitialDbContext = """
                                                using Dappi.HeadlessCms.Database;
                                                using Microsoft.EntityFrameworkCore;

                                                namespace Dappi.TestEnv.Data
                                                {
                                                    public class TestDbContext(DbContextOptions options) : DappiDbContext(options)
                                                    {
                                                        
                                                    }
                                                }

                                                """;

        public ModelsControllerTests(IntegrationWebAppFactory factory) : base(factory)
        {
            _entitiesPath = "Entities";
            _dbContextPath = "Data";

            IDbContextAccessor accessor = new DbContextAccessor<DappiDbContext>(DbContext);
            var domainModelEditor = new DomainModelEditor(_entitiesPath);
            var dbContextEditor = new DbContextEditor(_dbContextPath, "TestDbContext");
            ICurrentDappiSessionProvider sessionProvider = new CurrentDappiSessionProvider(new HttpContextAccessor());
            _controller = new ModelsController(accessor, sessionProvider, domainModelEditor, dbContextEditor);

            Directory.CreateDirectory(_entitiesPath);
            Directory.CreateDirectory(_dbContextPath);
            if (!File.Exists(Path.Combine(_dbContextPath, "TestDbContext.cs")))
            {
                File.WriteAllText(Path.Combine(_dbContextPath, "TestDbContext.cs"), InitialDbContext);
            }
        }

        [Fact, Order(1)]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Empty()
        {
            var request = new ModelRequest { ModelName = string.Empty, IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Theory, Order(2)]
        [ClassData(typeof(InvalidPropertyTypesAndClassNames))]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Invalid(string modelName)
        {
            var request = new ModelRequest { ModelName = modelName, IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact, Order(3)]
        public async Task CreateModel_Should_Create_Model_File()
        {
            var request = new ModelRequest { ModelName = "Product", IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            var filePath = Path.Combine(_entitiesPath, $"{request.ModelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            var actual = await File.ReadAllTextAsync(filePath);
            Assert.IsType<OkObjectResult>(res);
            Assert.NotNull(actual);
            Assert.Contains($$"""public DbSet<{{request.ModelName}}> {{request.ModelName.Pluralize()}} { get; set; }""", dbContext);
        }

        [Fact, Order(4)]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Already_Taken()
        {
            var request = new ModelRequest { ModelName = "Product", IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact, Order(5)]
        public async Task CreateModel_Should_Create_Model_File_With_Auditable_Props()
        {
            var request = new ModelRequest { ModelName = "InventoryItem", IsAuditableEntity = true };
            var res= await _controller.CreateModel(request);
            var filePath = Path.Combine(_entitiesPath, $"{request.ModelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            var actual = await File.ReadAllTextAsync(filePath);
            Assert.IsType<OkObjectResult>(res);
            Assert.NotNull(actual);
            Assert.Contains($"{request.ModelName} : {nameof(IAuditableEntity)}", actual);
            Assert.Contains("public DateTime? CreatedAtUtc { get; set; }", actual);
            Assert.Contains("public DateTime? UpdatedAtUtc { get; set; }", actual);
            Assert.Contains("public string? CreatedBy { get; set; }", actual);
            Assert.Contains("public string? UpdatedBy { get; set; }", actual);
            Assert.Contains($$"""public DbSet<{{request.ModelName}}> {{request.ModelName.Pluralize()}} { get; set; }""", dbContext);
        }

        [Fact, Order(6)]
        public async Task GetAllModels_Should_Return_All_Models()
        {
            var res = await _controller.GetAllModels();
            Assert.IsType<OkObjectResult>(res);
            var result = (OkObjectResult)res;
            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<List<string>>(result.Value);
            Assert.NotEmpty((List<string>)result.Value);
        }

        [Fact, Order(7)]
        public async Task AddField_Should_Return_BadRequest_If_Field_Name_Is_Empty()
        {
            var request = new FieldRequest
            {
                FieldName = string.Empty,
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null
            };
            var res = await _controller.AddField("Product", request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Theory, Order(8)]
        [ClassData(typeof(InvalidPropertyTypesAndClassNames))]
        public async Task AddField_Should_Return_BadRequest_If_Field_Name_Is_Invalid(string fieldName)
        {
            var request = new FieldRequest
            {
                FieldName = fieldName,
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null
            };
            var res = await _controller.AddField("Product", request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact, Order(9)]
        public async Task AddField_Should_Return_BadRequest_If_Field_Name_Is_Same_As_Model()
        {
            var request = new FieldRequest
            {
                FieldName = "Product",
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null
            };
            var res = await _controller.AddField("Product", request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact, Order(10)]
        public async Task AddField_Should_Return_NotFound_If_Model_Does_Not_Exist()
        {
            var request = new FieldRequest
            {
                FieldName = "Product",
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null
            };
            var res = await _controller.AddField("NotExistingClass", request);
            Assert.IsType<NotFoundObjectResult>(res);
        }


        [ClassData(typeof(TestFieldTypeAndFieldName))]
        [Theory, Order(11)]
        public async Task AddField_Should_Add_Required_Field_To_Model(string fieldName, string fieldType)
        {
            var model = new ModelRequest { ModelName = "ProductTwo", IsAuditableEntity = false };
            var request = new FieldRequest
            {
                FieldName = fieldName,
                FieldType = fieldType,
                RelatedTo = null,
                IsRequired = true,
                RelatedRelationName = null
            };
            await _controller.CreateModel(model);
            await _controller.AddField(model.ModelName, request);
            var filePath = Path.Combine(_entitiesPath, $"{model.ModelName}.cs");
            var actual = await File.ReadAllTextAsync(filePath);
            var expected = $$"""
                             public {{fieldType}} {{fieldName}} { get; set; }
                             """;
            Assert.Contains(expected, actual);
        }

        [ClassData(typeof(TestFieldTypeAndFieldName))]
        [Theory, Order(11)]
        public async Task AddField_Should_Add_Optional_Field_To_Model(string fieldName, string fieldType)
        {
            var model = new ModelRequest { ModelName = "ProductThree", IsAuditableEntity = false };
            var request = new FieldRequest
            {
                FieldName = fieldName,
                FieldType = fieldType,
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null
            };
            await _controller.CreateModel(model);
            await _controller.AddField(model.ModelName, request);
            var filePath = Path.Combine(_entitiesPath, $"{model.ModelName}.cs");
            var actual = await File.ReadAllTextAsync(filePath);
            var expected = $$"""
                             public {{fieldType}}? {{fieldName}} { get; set; }
                             """;
            Assert.Contains(expected, actual);
        }
        
        [Fact, Order(13)]
        public async Task AddField_Should_Add_OneToOne_Relation_To_Models()
        {
            var type1 = "TestClassOne";
            var type2 = "TestClassTwo";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToOne",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null
            };
            await _controller.CreateModel(new ModelRequest { ModelName = type1, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = type2, IsAuditableEntity = false });
            
            var res = await _controller.AddField(type1, request);
            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            
            Assert.Contains($$"""public {{type2}}? {{request.FieldName}} { get; set; }""", file1);
            Assert.Contains($$"""public {{type1}}? {{request.RelatedRelationName ?? type1}} { get; set; }""", file2);
            Assert.Contains($$"""public {{nameof(Guid)}}? {{type1}}Id { get; set; }""", file2);
            Assert.Contains($$"""public DbSet<{{type1}}> {{type1.Pluralize()}} { get; set; }""", dbContext);
            Assert.Contains($$"""public DbSet<{{type2}}> {{type2.Pluralize()}} { get; set; }""", dbContext);
            
            Assert.Contains($"modelBuilder.Entity<{type1}>()" , dbContext);
            Assert.Contains($".HasOne<{type2}>(s => s.{request.FieldName})", dbContext);
            Assert.Contains($".WithOne(e => e.{request.RelatedRelationName ?? type1})", dbContext);
            Assert.Contains($".HasForeignKey<{type2}>(ad => ad.{type1}Id)", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            Assert.IsType<OkObjectResult>(res);
        }

        [Fact , Order(14)]
        public async Task AddField_Should_Add_OneToMany_Relation_To_Models()
        {
            var type1 = "TestClassThree";
            var type2 = "TestClassFour";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToMany",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null
            };
            await _controller.CreateModel(new ModelRequest { ModelName = type1, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = type2, IsAuditableEntity = false });
            
            var res = await _controller.AddField(type1 , request);
            
            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2= await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            
            Assert.Contains($$"""public ICollection<{{type2}}>? {{request.FieldName}} { get; set; }""" , file1);
            Assert.Contains($$"""public {{type1}}? {{request.RelatedRelationName ?? type1}} { get; set; }""" , file2);
            Assert.Contains($$"""public {{nameof(Guid)}}? {{type1}}Id { get; set; }""" , file2);
            
            Assert.Contains($"modelBuilder.Entity<{type1}>()", dbContext);
            Assert.Contains($".HasMany<{type2}>(s => s.{request.FieldName})", dbContext);
            Assert.Contains($".WithOne(e => e.{request.RelatedRelationName ?? type1})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{request.RelatedRelationName ?? type1}Id);", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            Assert.IsType<OkObjectResult>(res);
        }
        
        [Fact , Order(15)]
        public async Task AddField_Should_Add_ManyToOne_Relation_To_Models()
        {
            var type1 = "TestClassFive";
            var type2 = "TestClassSix";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "ManyToOne",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null
            };
            await _controller.CreateModel(new ModelRequest { ModelName = type1, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = type2, IsAuditableEntity = false });
            
            var res = await _controller.AddField(type1 , request);
            
            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2= await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
        
            Assert.Contains($$"""public {{nameof(Guid)}}? {{request.FieldName}}Id { get; set; }""" , file1);
            Assert.Contains($$"""public {{type2}}? {{request.FieldName}} { get; set; }""" , file1);
            Assert.Contains($$"""public ICollection<{{type1}}>? {{request.RelatedRelationName ?? type1.Pluralize()}} { get; set; }""" , file2);
            
            Assert.Contains($"modelBuilder.Entity<{type1}>()", dbContext);
            Assert.Contains($".HasOne<{type2}>(s => s.{request.FieldName})", dbContext);
            Assert.Contains($".WithMany(e => e.{request.RelatedRelationName ?? $"{type1.Pluralize()}"})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{request.FieldName}Id);", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            Assert.IsType<OkObjectResult>(res);
        }
        
        [Fact , Order(16)]
        public async Task AddField_Should_Add_ManyToMany_Relation_To_Models()
        {
            var type1 = "TestClassSeven";
            var type2 = "TestClassEight";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "ManyToMany",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null
            };
            await _controller.CreateModel(new ModelRequest { ModelName = type1, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = type2, IsAuditableEntity = false });
            
            var res = await _controller.AddField(type1 , request);
            
            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2= await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            
            Assert.Contains($$"""public ICollection<{{type2}}>? {{request.FieldName}} { get; set; }""" , file1);
            Assert.Contains($$"""public ICollection<{{type1}}>? {{request.RelatedRelationName ?? type1.Pluralize()}} { get; set; }""" , file2);
            
            Assert.Contains($"modelBuilder.Entity<{type1}>()", dbContext);
            Assert.Contains($".HasMany(m => m.{request.FieldName})", dbContext);
            Assert.Contains($".WithMany(r => r.{type1.Pluralize()})", dbContext);
            Assert.Contains($".UsingEntity(j => j.ToTable(\"{type1}{type2.Pluralize()}\"));", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            Assert.IsType<OkObjectResult>(res);
        }
        public void Dispose()
        {
            // if (Directory.Exists(_entitiesPath))
            // {
            //     Directory.Delete(_entitiesPath, true);
            // }
            // if (Directory.Exists(_dbContextPath))
            // {
            //     Directory.Delete(_dbContextPath, true);
            // }
        }
    }
}