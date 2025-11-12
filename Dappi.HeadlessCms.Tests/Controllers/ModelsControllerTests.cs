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
            const string enumsPath = "Enums";
            _dbContextPath = "Data";

            IDbContextAccessor accessor = new DbContextAccessor<DappiDbContext>(DbContext);
            var domainModelEditor = new DomainModelEditor(_entitiesPath , enumsPath);
            var dbContextEditor = new DbContextEditor(_dbContextPath, "TestDbContext");
            ICurrentDappiSessionProvider sessionProvider = new CurrentDappiSessionProvider(new HttpContextAccessor());
            IContentTypeChangesService contentTypeChangesService = new ContentTypeChangesService(sessionProvider,accessor);
            _controller = new ModelsController(domainModelEditor, dbContextEditor , contentTypeChangesService);

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
            var request = new ModelRequest { ModelName = "DuplicateModel", IsAuditableEntity = false };
            await _controller.CreateModel(request);
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
            await _controller.CreateModel(new ModelRequest { ModelName = "TestModel1", IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = "TestModel2", IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = "TestModel3", IsAuditableEntity = false });
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
            Assert.Contains($".HasForeignKey(s => s.{type1}Id);", dbContext);
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
        
            Assert.Contains($$"""public {{nameof(Guid)}}? {{request.RelatedTo}}Id { get; set; }""" , file1);
            Assert.Contains($$"""public {{type2}}? {{request.FieldName}} { get; set; }""" , file1);
            Assert.Contains($$"""public ICollection<{{type1}}>? {{request.RelatedRelationName ?? type1.Pluralize()}} { get; set; }""" , file2);
            
            Assert.Contains($"modelBuilder.Entity<{type1}>()", dbContext);
            Assert.Contains($".HasOne<{type2}>(s => s.{request.FieldName})", dbContext);
            Assert.Contains($".WithMany(e => e.{request.RelatedRelationName ?? $"{type1.Pluralize()}"})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{type2}Id);", dbContext);
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
        
        [Fact]
        public async Task DeleteModel_Should_Return_NotFound_If_Model_Does_Not_Exist()
        {
            var res = await _controller.DeleteModel("NotExistingClass");
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task DeleteModel_Should_Return_BadRequest_If_ModelName_Is_Empty()
        {
            var res = await _controller.DeleteModel(string.Empty);
            Assert.IsType<BadRequestObjectResult>(res);
        }
        
        [Fact]
        public async Task DeleteModel_Should_Delete_Model_File()
        {
            var modelName = "TestDeleteModel";
            await _controller.CreateModel(new ModelRequest { ModelName = modelName, IsAuditableEntity = false });
            var res = await _controller.DeleteModel(modelName);
            var filePath = Path.Combine(_entitiesPath, $"{modelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            Assert.IsType<OkObjectResult>(res);
            Assert.False(File.Exists(filePath));
            Assert.DoesNotContain($"DbSet<{modelName}> {modelName.Pluralize()}", await File.ReadAllTextAsync(dbContextFilePath), StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task DeleteModel_Should_Delete_Relations_And_References()
        {
            const string user = "User";
            const string post = "Post";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToMany",
                RelatedTo = post,
                IsRequired = false,
                RelatedRelationName = null
            };
            await _controller.CreateModel(new ModelRequest { ModelName = user, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = post, IsAuditableEntity = false });
            
            var res = await _controller.AddField(user , request);
            
            var userFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{user}.cs"));
            var postFile= await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            
            Assert.Contains($$"""public ICollection<{{post}}>? {{request.FieldName}} { get; set; }""" , userFile);
            Assert.Contains($$"""public {{user}}? {{request.RelatedRelationName ?? user}} { get; set; }""" , postFile);
            Assert.Contains($$"""public {{nameof(Guid)}}? {{user}}Id { get; set; }""" , postFile);
            
            Assert.Contains($"modelBuilder.Entity<{user}>()", dbContext);
            Assert.Contains($".HasMany<{post}>(s => s.{request.FieldName})", dbContext);
            Assert.Contains($".WithOne(e => e.{request.RelatedRelationName ?? user})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{user}Id);", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            Assert.IsType<OkObjectResult>(res);
            
            var deleteRes = await _controller.DeleteModel(user);
            var updatedDbContextCode = await File.ReadAllTextAsync(dbContextFilePath);
            var updatedPostFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            
            Assert.DoesNotContain($$"""public {{user}}? {{request.RelatedRelationName ?? user}} { get; set; }""" , updatedPostFile);
            Assert.DoesNotContain($$"""public {{nameof(Guid)}}? {{user}}Id { get; set; }""", updatedPostFile);
            
            Assert.DoesNotContain($"DbSet<{user}> {user.Pluralize()}", updatedDbContextCode);
            Assert.DoesNotContain($"modelBuilder.Entity<{user}>()", updatedDbContextCode);
            Assert.DoesNotContain($".HasMany<{post}>(s => s.{request.FieldName})", updatedDbContextCode);
            Assert.DoesNotContain($".WithOne(e => e.{request.RelatedRelationName ?? user})", updatedDbContextCode);
            Assert.DoesNotContain($".HasForeignKey(s => s.{user}Id);", updatedDbContextCode);
           
            Assert.IsType<OkObjectResult>(deleteRes);
        }

        [Fact]
        public async Task Other_Relations_Should_Not_Be_Deleted()
        {
            const string user = "User";
            const string post = "Post";
            const string comment = "Comment";
            
            var postRequest = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToMany",
                RelatedTo = post,
                IsRequired = false,
                RelatedRelationName = null
            };

            var commentsRequest = new FieldRequest()
            {
                FieldName = "Comments",
                FieldType = "OneToMany",
                RelatedTo = comment,
                IsRequired = false,
            };
            
            await _controller.CreateModel(new ModelRequest { ModelName = user, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = post, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = comment, IsAuditableEntity = false });
            
            var res = await _controller.AddField(user , postRequest);
            await _controller.AddField(post , commentsRequest);
            await _controller.AddField(user, commentsRequest);
            
            var userFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{user}.cs"));
            var postFile= await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            var commentFile= await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{comment}.cs"));
            
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            
            Assert.Contains($$"""public ICollection<{{post}}>? {{postRequest.FieldName}} { get; set; }""" , userFile);
            Assert.Contains($$"""public ICollection<{{comment}}>? {{commentsRequest.FieldName}} { get; set; }""" , userFile);
            
            Assert.Contains($$"""public {{user}}? {{postRequest.RelatedRelationName ?? user}} { get; set; }""" , postFile);
            Assert.Contains($$"""public {{nameof(Guid)}}? {{user}}Id { get; set; }""" , postFile);
            Assert.Contains($$"""public ICollection<{{comment}}>? {{commentsRequest.FieldName}} { get; set; }""" , postFile);

            Assert.Contains($$"""public {{user}}? {{commentsRequest.RelatedRelationName ?? user}} { get; set; }""", commentFile);
            Assert.Contains($$"""public {{nameof(Guid)}}? {{user}}Id { get; set; }""", commentFile);
            Assert.Contains($$"""public {{post}}? {{commentsRequest.RelatedRelationName ?? post}} { get; set; }""" , commentFile);
            Assert.Contains($$"""public {{nameof(Guid)}}? {{post}}Id { get; set; }""" , commentFile);
            
            Assert.Contains($"modelBuilder.Entity<{user}>()", dbContext);
            Assert.Contains($".HasMany<{post}>(s => s.{postRequest.FieldName})", dbContext);
            Assert.Contains($".WithOne(e => e.{postRequest.RelatedRelationName ?? user})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{user}Id);", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            
            Assert.Contains($"modelBuilder.Entity<{user}>()", dbContext);
            Assert.Contains($".HasMany<{comment}>(s => s.{commentsRequest.FieldName})", dbContext);
            Assert.Contains($".WithOne(e => e.{commentsRequest.RelatedRelationName ?? post})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{post}Id);", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            
            Assert.Contains($"modelBuilder.Entity<{post}>()", dbContext);
            Assert.Contains($".HasMany<{comment}>(s => s.{commentsRequest.FieldName})", dbContext);
            Assert.Contains($".WithOne(e => e.{commentsRequest.RelatedRelationName ?? post})", dbContext);
            Assert.Contains($".HasForeignKey(s => s.{post}Id);", dbContext);
            Assert.Contains("base.OnModelCreating(modelBuilder);", dbContext);
            
            Assert.IsType<OkObjectResult>(res);
            
            var deleteRes = await _controller.DeleteModel(user);
            var updatedDbContextCode = await File.ReadAllTextAsync(dbContextFilePath);
            var updatedPostFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            var updatedCommentFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{comment}.cs"));
            
            Assert.DoesNotContain($"DbSet<{user}> {user.Pluralize()}", updatedDbContextCode);
            Assert.DoesNotContain($$"""public {{user}}? {{postRequest.RelatedRelationName ?? user}} { get; set; }""" , updatedPostFile);
            Assert.DoesNotContain($$"""public {{nameof(Guid)}}? {{user}}Id { get; set; }""", updatedPostFile);
            Assert.DoesNotContain($$"""public {{user}}? {{postRequest.RelatedRelationName ?? user}} { get; set; }""" , updatedPostFile);
            Assert.DoesNotContain($$"""public {{nameof(Guid)}}? {{user}}Id { get; set; }""", updatedCommentFile);
            
            Assert.DoesNotContain($"modelBuilder.Entity<{user}>()", updatedDbContextCode);
            Assert.DoesNotContain($".HasMany<{post}>(s => s.{postRequest.FieldName})", updatedDbContextCode);
            Assert.DoesNotContain($".WithOne(e => e.{postRequest.RelatedRelationName ?? user})", updatedDbContextCode);
            Assert.DoesNotContain($".HasForeignKey(s => s.{user}Id);", updatedDbContextCode);
            
            Assert.DoesNotContain($"modelBuilder.Entity<{user}>()", updatedDbContextCode);
            Assert.DoesNotContain($".HasMany<{comment}>(s => s.{commentsRequest.FieldName})", updatedDbContextCode);
            Assert.DoesNotContain($".WithOne(e => e.{commentsRequest.RelatedRelationName ?? post})", updatedDbContextCode);
            Assert.DoesNotContain($".HasForeignKey(s => s.{post}Id);", updatedDbContextCode);
            
            Assert.Contains($"modelBuilder.Entity<{post}>()", updatedDbContextCode);
            Assert.Contains($".HasMany<{comment}>(s => s.{commentsRequest.FieldName})", updatedDbContextCode);
            Assert.Contains($".WithOne(e => e.{commentsRequest.RelatedRelationName ?? post})", updatedDbContextCode);
            Assert.Contains($".HasForeignKey(s => s.{post}Id);", updatedDbContextCode);
            Assert.Contains("base.OnModelCreating(modelBuilder);", updatedDbContextCode);
            
            Assert.IsType<OkObjectResult>(deleteRes);
        }
        
        public void Dispose()
        {
            if (Directory.Exists(_entitiesPath))
            {
                Directory.Delete(_entitiesPath, true);
            }
            if (Directory.Exists(_dbContextPath))
            {
                Directory.Delete(_dbContextPath, true);
            }
        }
    }
}