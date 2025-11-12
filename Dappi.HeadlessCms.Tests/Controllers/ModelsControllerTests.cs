using Dappi.HeadlessCms.Controllers;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Services;
using Dappi.HeadlessCms.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dappi.HeadlessCms.Tests.Controllers
{
    public class ModelsControllerTests : BaseIntegrationTest, IDisposable
    {
        private readonly ModelsController _controller;
        private readonly string _entitiesPath;
        private readonly string _dbContextPath;
        private readonly string _snapshotPath = $"../snapshots/{nameof(ModelsControllerTests)}";

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
            var domainModelEditor = new DomainModelEditor(_entitiesPath, enumsPath);
            var dbContextEditor = new DbContextEditor(_dbContextPath, "TestDbContext");
            ICurrentDappiSessionProvider sessionProvider = new CurrentDappiSessionProvider(new HttpContextAccessor());
            IContentTypeChangesService contentTypeChangesService =
                new ContentTypeChangesService(sessionProvider, accessor);
            _controller = new ModelsController(domainModelEditor, dbContextEditor, contentTypeChangesService);

            Directory.CreateDirectory(_entitiesPath);
            Directory.CreateDirectory(_dbContextPath);
            if (!File.Exists(Path.Combine(_dbContextPath, "TestDbContext.cs")))
            {
                File.WriteAllText(Path.Combine(_dbContextPath, "TestDbContext.cs"), InitialDbContext);
            }
        }

        [Fact]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Empty()
        {
            var request = new ModelRequest { ModelName = string.Empty, IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Theory]
        [ClassData(typeof(InvalidPropertyTypesAndClassNames))]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Invalid(string modelName)
        {
            var request = new ModelRequest { ModelName = modelName, IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task CreateModel_Should_Create_Model_File()
        {
            var request = new ModelRequest { ModelName = "Product", IsAuditableEntity = false };
            var res = await _controller.CreateModel(request);
            var filePath = Path.Combine(_entitiesPath, $"{request.ModelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            var actual = await File.ReadAllTextAsync(filePath);

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(CreateModel_Should_Create_Model_File)}");

            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(actual, verifySettings).UseFileName("model");
            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Already_Taken()
        {
            var request = new ModelRequest { ModelName = "DuplicateModel", IsAuditableEntity = false };
            await _controller.CreateModel(request);
            var res = await _controller.CreateModel(request);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task CreateModel_Should_Create_Model_File_With_Auditable_Props()
        {
            var request = new ModelRequest { ModelName = "InventoryItem", IsAuditableEntity = true };
            var res = await _controller.CreateModel(request);
            var filePath = Path.Combine(_entitiesPath, $"{request.ModelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            var actual = await File.ReadAllTextAsync(filePath);

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(CreateModel_Should_Create_Model_File_With_Auditable_Props)}");
            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(actual, verifySettings).UseFileName("model");
            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task GetAllModels_Should_Return_All_Models()
        {
            await _controller.CreateModel(new ModelRequest { ModelName = "TestModel1", IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = "TestModel2", IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = "TestModel3", IsAuditableEntity = false });
            var res = await _controller.GetAllModels();

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(GetAllModels_Should_Return_All_Models)}");

            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
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

        [Theory]
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

        [Fact]
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

        [Fact]
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
        [Theory]
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
        [Theory]
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

        [Fact]
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

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(AddField_Should_Add_OneToOne_Relation_To_Models)}");
            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(file1, verifySettings).UseFileName("file1");
            await Verify(file2, verifySettings).UseFileName("file2");
            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
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

            var res = await _controller.AddField(type1, request);

            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(AddField_Should_Add_OneToMany_Relation_To_Models)}");
            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(file1, verifySettings).UseFileName("file1");
            await Verify(file2, verifySettings).UseFileName("file2");
            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
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

            var res = await _controller.AddField(type1, request);

            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(AddField_Should_Add_ManyToOne_Relation_To_Models)}");
            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(file1, verifySettings).UseFileName("file1");
            await Verify(file2, verifySettings).UseFileName("file2");
            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
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

            var res = await _controller.AddField(type1, request);

            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(AddField_Should_Add_ManyToMany_Relation_To_Models)}");
            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(file1, verifySettings).UseFileName("file1");
            await Verify(file2, verifySettings).UseFileName("file2");
            await Verify(res, verifySettings).UseFileName("response");
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
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            
            Assert.False(File.Exists(filePath));
            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(DeleteModel_Should_Delete_Model_File)}");
            await Verify(dbContext, verifySettings).UseFileName("dbContext");
            await Verify(res, verifySettings).UseFileName("response");
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

            await _controller.AddField(user, request);

            await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{user}.cs"));
            await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");

            var res = await _controller.DeleteModel(user);
            var updatedDbContextCode = await File.ReadAllTextAsync(dbContextFilePath);
            var updatedPostFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            
            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(DeleteModel_Should_Delete_Relations_And_References)}");
            await Verify(updatedDbContextCode, verifySettings).UseFileName("dbContext");
            await Verify(updatedPostFile, verifySettings).UseFileName(post);
            await Verify(res, verifySettings).UseFileName("response");
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
                FieldName = "Comments", FieldType = "OneToMany", RelatedTo = comment, IsRequired = false,
            };

            await _controller.CreateModel(new ModelRequest { ModelName = user, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = post, IsAuditableEntity = false });
            await _controller.CreateModel(new ModelRequest { ModelName = comment, IsAuditableEntity = false });

            await _controller.AddField(user, postRequest);
            await _controller.AddField(post, commentsRequest);
            await _controller.AddField(user, commentsRequest);

            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");

            var deleteRes = await _controller.DeleteModel(user);
            var updatedDbContextCode = await File.ReadAllTextAsync(dbContextFilePath);
            var updatedPostFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            var updatedCommentFile = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{comment}.cs"));

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory($"{_snapshotPath}/{nameof(Other_Relations_Should_Not_Be_Deleted)}");
            
            await Verify(updatedDbContextCode, verifySettings).UseFileName("dbContext");
            await Verify(updatedPostFile, verifySettings).UseFileName(post);
            await Verify(updatedCommentFile, verifySettings).UseFileName(comment);
            await Verify(deleteRes, verifySettings).UseFileName("response");
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