using Bogus;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.Extensions;

namespace Dappi.HeadlessCms.Tests.DataShaping
{
    internal class DataShapingDummyModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        private string PrivateField { get; set; } = "Private";
        protected string ProtectedField { get; set; } = "Protected";
        internal string InternalField { get; set; } = "Internal";
        
        public static Faker<DataShapingDummyModel> Faker { get; } = new Faker<DataShapingDummyModel>()
            .RuleFor(x => x.Id, f => Guid.NewGuid())
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.CreatedAt, f => f.Date.Past())
            .RuleFor(x => x.IsDeleted, f => f.Random.Bool());
    }
    
    public class DataShapingTests
    {
        private readonly DataShapingDummyModel _dummyModel = DataShapingDummyModel.Faker.Generate();
        private readonly List<DataShapingDummyModel> _dummyModels = DataShapingDummyModel.Faker.Generate(10);

        [Fact]
        public void ShapeObject_Should_Throw_If_Property_Does_Not_Exist()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModel.ShapeObject("NonExistingProperty"));
        }

        [Fact]
        public void ShapeObject_Should_Throw_For_Private_Properties()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModel.ShapeObject("PrivateField"));
        }
        
        [Fact]
        public void ShapeObject_Should_Throw_For_Protected_Properties()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModel.ShapeObject("ProtectedField"));
        }
        
        [Fact]
        public void ShapeObject_Should_Throw_For_Internal_Properties()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModel.ShapeObject("InternalField"));
        }
        
        [Fact]
        public void ShapeObject_Should_Return_All_Properties_If_Fields_Param_Is_Null()
        {
            IDictionary<string,object?> expandoObject = _dummyModel.ShapeObject(null);
            
            Assert.Equal(4, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
            Assert.Contains("IsDeleted",expandoObject.Keys);
            Assert.Contains("CreatedAt",expandoObject.Keys);
        }
        
        [Fact]
        public void ShapeObject_Should_Return_All_Properties_If_Fields_Param_Is_EmptyString()
        {
            IDictionary<string,object?> expandoObject = _dummyModel.ShapeObject(string.Empty);
            
            Assert.Equal(4, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
            Assert.Contains("IsDeleted",expandoObject.Keys);
            Assert.Contains("CreatedAt",expandoObject.Keys);
        }

        [Fact]
        public void ShapeObject_Should_Return_Only_Selected_Properties()
        {
            const string fields = "Id,Name";
            IDictionary<string,object?> expandoObject = _dummyModel.ShapeObject(fields);
            
            Assert.Equal(fields.Split(',').Length, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
        }
        
        [Fact]
        public void ShapeObject_Should_Ignore_Case_Selected_Properties()
        {
            const string fields = "Id,nAMe";
            IDictionary<string,object?> expandoObject = _dummyModel.ShapeObject(fields);
            
            Assert.Equal(fields.Split(',').Length, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
        }
        
        [Fact]
        public void ShapeObject_Should_Return_Correct_Values_For_All_Properties()
        {
            IDictionary<string,object?> expandoObject = _dummyModel.ShapeObject(null);
            
            Assert.Equal(4,expandoObject.Count);
            Assert.Equal(_dummyModel.Id, expandoObject["Id"]);
            Assert.Equal(_dummyModel.Name, expandoObject["Name"]);
            Assert.Equal(_dummyModel.IsDeleted, expandoObject["IsDeleted"]);
            Assert.Equal(_dummyModel.CreatedAt, expandoObject["CreatedAt"]);
        }
        
        [Fact]
        public void ShapeObject_Should_Return_Correct_Values_For_Selected_Properties()
        {
            const string fields = "Id,Name,IsDeleted";
            IDictionary<string,object?> expandoObject = _dummyModel.ShapeObject("id,name,isDeleted");
            
            Assert.Equal(fields.Split(',').Length, expandoObject.Count);
            Assert.Equal(_dummyModel.Id, expandoObject["Id"]);
            Assert.Equal(_dummyModel.Name, expandoObject["Name"]);
            Assert.Equal(_dummyModel.IsDeleted, expandoObject["IsDeleted"]);
        }
        
        [Fact] 
        public void ShapeData_Should_Throw_If_Property_Does_Not_Exist()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModels.ShapeData("NonExistingProperty"));
        }
        
        [Fact]
        public void ShapeData_Should_Throw_For_Private_Properties()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModels.ShapeData("PrivateField"));
        }
        
        [Fact]
        public void ShapeData_Should_Throw_For_Protected_Properties()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModels.ShapeData("ProtectedField"));
        }
        
        [Fact]
        public void ShapeData_Should_Throw_For_Internal_Properties()
        {
            Assert.Throws<PropertyNotFoundException>(() => _dummyModels.ShapeData("InternalField"));
        }
        
        [Fact]
        public void ShapeData_Should_Return_All_Properties_If_Fields_Param_Is_Null()
        {
            IEnumerable<IDictionary<string,object?>> expandoObjects = _dummyModels.ShapeData(null);

            foreach (var expandoObject in expandoObjects)
            {
                Assert.Equal(4, expandoObject.Count);
                Assert.Contains("Id",expandoObject.Keys);
                Assert.Contains("Name",expandoObject.Keys);
                Assert.Contains("IsDeleted",expandoObject.Keys);
                Assert.Contains("CreatedAt",expandoObject.Keys);
            }
        }
        
        [Fact]
        public void ShapeData_Should_Return_All_Properties_If_Fields_Param_Is_EmptyString()
        {
            IEnumerable<IDictionary<string,object?>> expandoObjects = _dummyModels.ShapeData(string.Empty);

            foreach (var expandoObject in expandoObjects)
            {
                Assert.Equal(4, expandoObject.Count);
                Assert.Contains("Id",expandoObject.Keys);
                Assert.Contains("Name",expandoObject.Keys);
                Assert.Contains("IsDeleted",expandoObject.Keys);
                Assert.Contains("CreatedAt",expandoObject.Keys);
            }
        }
        
        [Fact]
        public void ShapeData_Should_Return_Selected_Properties()
        {
            const string fields = "Id,Name";
            IEnumerable<IDictionary<string,object?>> expandoObjects = _dummyModels.ShapeData(fields);

            foreach (var expandoObject in expandoObjects)
            {
                Assert.Equal(fields.Split(',').Length, expandoObject.Count);
                Assert.Contains("Id",expandoObject.Keys);
                Assert.Contains("Name",expandoObject.Keys);
            }
        }
        
        [Fact]
        public void ShapeData_Should_Ignore_Case()
        {
            const string fields = "iD,nAMe";
            IEnumerable<IDictionary<string,object?>> expandoObjects = _dummyModels.ShapeData(fields);

            foreach (var expandoObject in expandoObjects)
            {
                Assert.Equal(fields.Split(',').Length, expandoObject.Count);
                Assert.Contains("Id",expandoObject.Keys);
                Assert.Contains("Name",expandoObject.Keys);
            }
        }
        
        [Fact]
        public void ShapeData_Should_Return_Correct_Values_For_All_Properties()
        {
            IEnumerable<IDictionary<string,object?>> expandoObjects = _dummyModels.ShapeData(null).ToList();
            for (var i = 0; i < _dummyModels.Count; i++)
            {
                var expandoObject = expandoObjects.ElementAt(i);
                Assert.Equal(4,expandoObject.Count);
                Assert.Equal(_dummyModels[i].Id, expandoObject["Id"]);
                Assert.Equal(_dummyModels[i].Name, expandoObject["Name"]);
                Assert.Equal(_dummyModels[i].IsDeleted, expandoObject["IsDeleted"]);
                Assert.Equal(_dummyModels[i].CreatedAt, expandoObject["CreatedAt"]);
            }
        }
        
        [Fact]
        public void ShapeData_Should_Return_Correct_Values_For_Selected_Properties()
        {
            const string fields = "Id,Name,IsDeleted";
            IEnumerable<IDictionary<string,object?>> expandoObjects = _dummyModels.ShapeData(fields).ToList();
            for (var i = 0; i < _dummyModels.Count; i++)
            {
                var expandoObject = expandoObjects.ElementAt(i);
                Assert.Equal(fields.Split(',').Length,expandoObject.Count);
                Assert.Equal(_dummyModels[i].Id, expandoObject["Id"]);
                Assert.Equal(_dummyModels[i].Name, expandoObject["Name"]);
                Assert.Equal(_dummyModels[i].IsDeleted, expandoObject["IsDeleted"]);
            }
        }
    }
}