using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Tests.Auth;
using Dappi.HeadlessCms.Tests.TestData;

namespace Dappi.HeadlessCms.Tests.Controllers
{
    public class ModelsControllerTests : BaseIntegrationTestFixture, IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _entitiesPath;
        private readonly string _dbContextPath;
        private readonly string _baseUrl = "/api/models";
        private readonly string _snapshotPath = $"../snapshots/{nameof(ModelsControllerTests)}";
        private readonly VerifySettings _verifySettings;

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

        public ModelsControllerTests(IntegrationWebAppFactory factory)
            : base(factory)
        {
            _client = factory.CreateClient();

            _entitiesPath = "Entities";
            _dbContextPath = "Data";
            _verifySettings = new VerifySettings();
            _verifySettings.ScrubMember("traceId");

            _verifySettings.ScrubLinesWithReplace(s =>
                Regex.Replace(s, @"^.*\busing\b.*\.Entities.*$", "using Entities;")
            );

            _verifySettings.ScrubLinesWithReplace(s =>
                Regex.Replace(s, @"^.*\bnamespace\b.*\.Entities.*$", "namespace Entities")
            );

            Directory.CreateDirectory(_entitiesPath);
            Directory.CreateDirectory(_dbContextPath);
            if (!File.Exists(Path.Combine(_dbContextPath, "TestDbContext.cs")))
            {
                File.WriteAllText(
                    Path.Combine(_dbContextPath, "TestDbContext.cs"),
                    InitialDbContext
                );
            }
        }

        [Fact]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Empty()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new ModelRequest { ModelName = string.Empty, IsAuditableEntity = false };
            var res = await _client.PostAsJsonAsync(_baseUrl, request);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Empty)}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Theory]
        [ClassData(typeof(InvalidPropertyTypesAndClassNames))]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Invalid(
            string modelName
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new ModelRequest { ModelName = modelName, IsAuditableEntity = false };
            var res = await _client.PostAsJsonAsync(_baseUrl, request);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Invalid)}/{modelName}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task CreateModel_Should_Create_Model_File()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new ModelRequest { ModelName = "Product", IsAuditableEntity = false };
            var res = await _client.PostAsJsonAsync(_baseUrl, request);
            var filePath = Path.Combine(_entitiesPath, $"{request.ModelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            var actual = await File.ReadAllTextAsync(filePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(CreateModel_Should_Create_Model_File)}"
            );

            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(actual, _verifySettings).UseFileName("model");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Already_Taken()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new ModelRequest
            {
                ModelName = "DuplicateModel",
                IsAuditableEntity = false,
            };
            await _client.PostAsJsonAsync(_baseUrl, request);
            var res = await _client.PostAsJsonAsync(_baseUrl, request);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(CreateModel_Should_Return_BadRequest_If_Model_Name_Is_Already_Taken)}"
            );
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task CreateModel_Should_Create_Model_File_With_Auditable_Props()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new ModelRequest
            {
                ModelName = "InventoryItem",
                IsAuditableEntity = true,
            };
            var res = await _client.PostAsJsonAsync(_baseUrl, request);
            var filePath = Path.Combine(_entitiesPath, $"{request.ModelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);
            var actual = await File.ReadAllTextAsync(filePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(CreateModel_Should_Create_Model_File_With_Auditable_Props)}"
            );
            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(actual, _verifySettings).UseFileName("model");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task GetAllModels_Should_Return_All_Models()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = "TestModel1", IsAuditableEntity = false }
            );
            var res = await _client.GetAsync(_baseUrl);

            var verifySettings = new VerifySettings();
            verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(GetAllModels_Should_Return_All_Models)}"
            );

            await Verify(res, verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task AddField_Should_Return_BadRequest_If_Field_Name_Is_Empty()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new FieldRequest
            {
                FieldName = string.Empty,
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null,
            };
            var res = await _client.PutAsJsonAsync($"{_baseUrl}/Product", request);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Return_BadRequest_If_Field_Name_Is_Empty)}"
            );
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Theory]
        [ClassData(typeof(InvalidPropertyTypesAndClassNames))]
        public async Task AddField_Should_Return_BadRequest_If_Field_Name_Is_Invalid(
            string fieldName
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new FieldRequest
            {
                FieldName = fieldName,
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null,
            };

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/Product", request);
            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Return_BadRequest_If_Field_Name_Is_Invalid)}/{fieldName}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task AddField_Should_Return_BadRequest_If_Field_Name_Is_Same_As_Model()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new FieldRequest
            {
                FieldName = "Product",
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null,
            };

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/Product", request);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Return_BadRequest_If_Field_Name_Is_Same_As_Model)}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task AddField_Should_Return_NotFound_If_Model_Does_Not_Exist()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var request = new FieldRequest
            {
                FieldName = "Product",
                FieldType = "string",
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null,
            };

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/NotExistingClass", request);
            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Return_NotFound_If_Model_Does_Not_Exist)}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
        }

        [ClassData(typeof(TestFieldTypeAndFieldName))]
        [Theory]
        public async Task AddField_Should_Add_Required_Field_To_Model(
            string fieldName,
            string fieldType
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var model = new ModelRequest { ModelName = "ProductTwo", IsAuditableEntity = false };
            var request = new FieldRequest
            {
                FieldName = fieldName,
                FieldType = fieldType,
                RelatedTo = null,
                IsRequired = true,
                RelatedRelationName = null,
            };

            await _client.PostAsJsonAsync(_baseUrl, model);
            var res = await _client.PutAsJsonAsync($"{_baseUrl}/{model.ModelName}", request);
            var filePath = Path.Combine(_entitiesPath, $"{model.ModelName}.cs");
            var actual = await File.ReadAllTextAsync(filePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Add_Required_Field_To_Model)}/{fieldName}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
            await Verify(actual, _verifySettings).UseFileName("model");
        }

        [ClassData(typeof(TestFieldTypeAndFieldName))]
        [Theory]
        public async Task AddField_Should_Add_Optional_Field_To_Model(
            string fieldName,
            string fieldType
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var model = new ModelRequest { ModelName = "ProductThree", IsAuditableEntity = false };
            var request = new FieldRequest
            {
                FieldName = fieldName,
                FieldType = fieldType,
                RelatedTo = null,
                IsRequired = false,
                RelatedRelationName = null,
            };

            await _client.PostAsJsonAsync(_baseUrl, model);
            var res = await _client.PutAsJsonAsync($"{_baseUrl}/{model.ModelName}", request);
            var filePath = Path.Combine(_entitiesPath, $"{model.ModelName}.cs");
            var actual = await File.ReadAllTextAsync(filePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Add_Optional_Field_To_Model)}/{fieldName}"
            );

            await Verify(res, _verifySettings).UseFileName("response");
            await Verify(actual, _verifySettings).UseFileName("model");
        }

        [Fact]
        public async Task AddField_Should_Add_OneToOne_Relation_To_Models()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var type1 = "TestClassOne";
            var type2 = "TestClassTwo";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToOne",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null,
            };
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type1, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type2, IsAuditableEntity = false }
            );

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/{type1}", request);
            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Add_OneToOne_Relation_To_Models)}"
            );
            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(file1, _verifySettings).UseFileName("file1");
            await Verify(file2, _verifySettings).UseFileName("file2");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task AddField_Should_Add_OneToMany_Relation_To_Models()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var type1 = "TestClassThree";
            var type2 = "TestClassFour";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToMany",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null,
            };
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type1, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type2, IsAuditableEntity = false }
            );

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/{type1}", request);

            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Add_OneToMany_Relation_To_Models)}"
            );
            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(file1, _verifySettings).UseFileName("file1");
            await Verify(file2, _verifySettings).UseFileName("file2");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task AddField_Should_Add_ManyToOne_Relation_To_Models()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var type1 = "TestClassFive";
            var type2 = "TestClassSix";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "ManyToOne",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null,
            };
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type1, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type2, IsAuditableEntity = false }
            );

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/{type1}", request);

            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Add_ManyToOne_Relation_To_Models)}"
            );
            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(file1, _verifySettings).UseFileName("file1");
            await Verify(file2, _verifySettings).UseFileName("file2");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task AddField_Should_Add_ManyToMany_Relation_To_Models()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var type1 = "TestClassSeven";
            var type2 = "TestClassEight";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "ManyToMany",
                RelatedTo = type2,
                IsRequired = false,
                RelatedRelationName = null,
            };
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type1, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = type2, IsAuditableEntity = false }
            );

            var res = await _client.PutAsJsonAsync($"{_baseUrl}/{type1}", request);

            var file1 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type1}.cs"));
            var file2 = await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{type2}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Add_ManyToMany_Relation_To_Models)}"
            );
            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(file1, _verifySettings).UseFileName("file1");
            await Verify(file2, _verifySettings).UseFileName("file2");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Theory]
        [ClassData(typeof(ValidMinMaxConstraints))]
        public async Task AddField_Should_Accept_Valid_MinMax_Constraints(
            string fieldType,
            double? min,
            double? max,
            string testName
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var modelRequest = new ModelRequest
            {
                ModelName = $"MinMaxTest{testName}",
                IsAuditableEntity = false,
            };
            await _client.PostAsJsonAsync(_baseUrl, modelRequest);

            var fieldRequest = new FieldRequest
            {
                FieldName = "TestField",
                FieldType = fieldType,
                IsRequired = false,
                Min = min,
                Max = max,
            };

            var res = await _client.PutAsJsonAsync(
                $"{_baseUrl}/{modelRequest.ModelName}",
                fieldRequest
            );
            var filePath = Path.Combine(_entitiesPath, $"{modelRequest.ModelName}.cs");
            var actual = await File.ReadAllTextAsync(filePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Accept_Valid_MinMax_Constraints)}/{testName}"
            );
            await Verify(actual, _verifySettings).UseFileName("model");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Theory]
        [ClassData(typeof(InvalidMinMaxConstraints))]
        public async Task AddField_Should_Return_BadRequest_For_Invalid_MinMax_Constraints(
            string fieldType,
            double? min,
            double? max,
            string testName
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var modelRequest = new ModelRequest
            {
                ModelName = $"InvalidMinMax{testName}",
                IsAuditableEntity = false,
            };
            await _client.PostAsJsonAsync(_baseUrl, modelRequest);

            var fieldRequest = new FieldRequest
            {
                FieldName = "TestField",
                FieldType = fieldType,
                IsRequired = false,
                Min = min,
                Max = max,
            };

            var res = await _client.PutAsJsonAsync(
                $"{_baseUrl}/{modelRequest.ModelName}",
                fieldRequest
            );

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(AddField_Should_Return_BadRequest_For_Invalid_MinMax_Constraints)}/{testName}"
            );
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Theory]
        [ClassData(typeof(InvalidMinMaxConstraints))]
        public async Task UpdateField_Should_Return_BadRequest_For_Invalid_MinMax_Update(
            string fieldType,
            double? min,
            double? max,
            string testName
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var modelRequest = new ModelRequest
            {
                ModelName = $"UpdateInvalidMinMax{testName}",
                IsAuditableEntity = false,
            };
            await _client.PostAsJsonAsync(_baseUrl, modelRequest);

            var fieldRequest = new FieldRequest
            {
                FieldName = "TestField",
                FieldType = fieldType,
                IsRequired = false,
                Min = 0,
                Max = 100,
            };
            await _client.PutAsJsonAsync($"{_baseUrl}/{modelRequest.ModelName}", fieldRequest);

            var updateRequest = new UpdateFieldRequest
            {
                OldFieldName = "TestField",
                NewFieldName = "TestField",
                FieldType = fieldType,
                IsRequired = false,
                Min = min,
                Max = max,
            };

            var res = await PatchAsJsonAsync(
                _client,
                $"{_baseUrl}/{modelRequest.ModelName}/fields",
                updateRequest
            );

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(UpdateField_Should_Return_BadRequest_For_Invalid_MinMax_Update)}/{testName}"
            );
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Theory]
        [ClassData(typeof(ValidMinMaxConstraints))]
        public async Task UpdateField_Should_Accept_Valid_MinMax_Update(
            string fieldType,
            double? min,
            double? max,
            string testName
        )
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var modelRequest = new ModelRequest
            {
                ModelName = $"UpdateValidMinMax{testName}",
                IsAuditableEntity = false,
            };
            await _client.PostAsJsonAsync(_baseUrl, modelRequest);

            var fieldRequest = new FieldRequest
            {
                FieldName = "TestField",
                FieldType = fieldType,
                IsRequired = false,
                Min = 0,
                Max = 10,
            };
            await _client.PutAsJsonAsync($"{_baseUrl}/{modelRequest.ModelName}", fieldRequest);

            var updateRequest = new UpdateFieldRequest
            {
                OldFieldName = "TestField",
                NewFieldName = "TestField",
                FieldType = fieldType,
                IsRequired = false,
                Min = min,
                Max = max,
            };

            var res = await PatchAsJsonAsync(
                _client,
                $"{_baseUrl}/{modelRequest.ModelName}/fields",
                updateRequest
            );
            var filePath = Path.Combine(_entitiesPath, $"{modelRequest.ModelName}.cs");
            var actual = await File.ReadAllTextAsync(filePath);

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(UpdateField_Should_Accept_Valid_MinMax_Update)}/{testName}"
            );
            await Verify(actual, _verifySettings).UseFileName("model");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task DeleteModel_Should_Return_NotFound_If_Model_Does_Not_Exist()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var res = await _client.DeleteAsync($"{_baseUrl}/NotExistingClass");
            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(DeleteModel_Should_Return_NotFound_If_Model_Does_Not_Exist)}"
            );
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task DeleteModel_Should_Return_BadRequest_If_ModelName_Is_Empty()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var res = await _client.DeleteAsync($"{_baseUrl}/{string.Empty}");
            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(DeleteModel_Should_Return_BadRequest_If_ModelName_Is_Empty)}"
            );
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task DeleteModel_Should_Delete_Model_File()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            var modelName = "TestDeleteModel";
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = modelName, IsAuditableEntity = false }
            );
            var res = await _client.DeleteAsync($"{_baseUrl}/{modelName}");
            var filePath = Path.Combine(_entitiesPath, $"{modelName}.cs");
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");
            var dbContext = await File.ReadAllTextAsync(dbContextFilePath);

            Assert.False(File.Exists(filePath));
            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(DeleteModel_Should_Delete_Model_File)}"
            );
            await Verify(dbContext, _verifySettings).UseFileName("dbContext");
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task DeleteModel_Should_Delete_Relations_And_References()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            const string user = "User";
            const string post = "Post";
            var request = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToMany",
                RelatedTo = post,
                IsRequired = false,
                RelatedRelationName = null,
            };

            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = user, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = post, IsAuditableEntity = false }
            );

            await _client.PutAsJsonAsync($"{_baseUrl}/{user}", request);

            await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{user}.cs"));
            await File.ReadAllTextAsync(Path.Combine(_entitiesPath, $"{post}.cs"));
            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");

            var res = await _client.DeleteAsync($"{_baseUrl}/{user}");
            var updatedDbContextCode = await File.ReadAllTextAsync(dbContextFilePath);
            var updatedPostFile = await File.ReadAllTextAsync(
                Path.Combine(_entitiesPath, $"{post}.cs")
            );

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(DeleteModel_Should_Delete_Relations_And_References)}"
            );
            await Verify(updatedDbContextCode, _verifySettings).UseFileName("dbContext");
            await Verify(updatedPostFile, _verifySettings).UseFileName(post);
            await Verify(res, _verifySettings).UseFileName("response");
        }

        [Fact]
        public async Task Other_Relations_Should_Not_Be_Deleted()
        {
            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {auth?.Token}");

            const string user = "User";
            const string post = "Post";
            const string comment = "Comment";

            var postRequest = new FieldRequest
            {
                FieldName = "TestRelation",
                FieldType = "OneToMany",
                RelatedTo = post,
                IsRequired = false,
                RelatedRelationName = null,
            };

            var commentsRequest = new FieldRequest()
            {
                FieldName = "Comments",
                FieldType = "OneToMany",
                RelatedTo = comment,
                IsRequired = false,
            };

            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = user, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = post, IsAuditableEntity = false }
            );
            await _client.PostAsJsonAsync(
                _baseUrl,
                new ModelRequest { ModelName = comment, IsAuditableEntity = false }
            );

            await _client.PutAsJsonAsync($"{_baseUrl}/{user}", postRequest);
            await _client.PutAsJsonAsync($"{_baseUrl}/{post}", commentsRequest);
            await _client.PutAsJsonAsync($"{_baseUrl}/{user}", commentsRequest);

            var dbContextFilePath = Path.Combine(_dbContextPath, "TestDbContext.cs");

            var deleteRes = await _client.DeleteAsync($"{_baseUrl}/{user}");
            var updatedDbContextCode = await File.ReadAllTextAsync(dbContextFilePath);
            var updatedPostFile = await File.ReadAllTextAsync(
                Path.Combine(_entitiesPath, $"{post}.cs")
            );
            var updatedCommentFile = await File.ReadAllTextAsync(
                Path.Combine(_entitiesPath, $"{comment}.cs")
            );

            _verifySettings.UseDirectory(
                $"{_snapshotPath}/{nameof(Other_Relations_Should_Not_Be_Deleted)}"
            );

            await Verify(updatedDbContextCode, _verifySettings).UseFileName("dbContext");
            await Verify(updatedPostFile, _verifySettings).UseFileName(post);
            await Verify(updatedCommentFile, _verifySettings).UseFileName(comment);
            await Verify(deleteRes, _verifySettings).UseFileName("response");
        }

        private static Task<HttpResponseMessage> PatchAsJsonAsync<T>(
            HttpClient client,
            string requestUri,
            T value
        )
        {
            var content = JsonContent.Create(value);
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = content,
            };

            return client.SendAsync(request);
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
