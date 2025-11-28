using Bogus;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.Services;

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
            var shaper = new DataShaper();
            Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(_dummyModels,"NonExistingProperty"));
        }

        [Fact]
        public void ShapeObject_Should_Throw_For_Private_Properties()
        {
            var shaper = new DataShaper();
            Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(_dummyModel,"PrivateField"));
        }
        
        [Fact]
        public void ShapeObject_Should_Throw_For_Protected_Properties()
        {
            var shaper = new DataShaper();
            Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(_dummyModel,"ProtectedField"));
        }
        
        [Fact]
        public void ShapeObject_Should_Throw_For_Internal_Properties()
        {
            var shaper = new DataShaper();
            Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(_dummyModel,"InternalField"));
        }
        
        [Fact]
        public void ShapeObject_Should_Return_All_Properties_If_Fields_Param_Is_Null()
        {
            var shaper = new DataShaper();
            IDictionary<string,object?> expandoObject = shaper.ShapeObject( _dummyModel,null);
            
            Assert.Equal(4, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
            Assert.Contains("IsDeleted",expandoObject.Keys);
            Assert.Contains("CreatedAt",expandoObject.Keys);
        }
        
        [Fact]
        public void ShapeObject_Should_Return_All_Properties_If_Fields_Param_Is_EmptyString()
        {
            var shaper = new DataShaper();
            IDictionary<string,object?> expandoObject =  shaper.ShapeObject(_dummyModel,string.Empty);
            
            Assert.Equal(4, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
            Assert.Contains("IsDeleted",expandoObject.Keys);
            Assert.Contains("CreatedAt",expandoObject.Keys);
        }

        [Fact]
        public void ShapeObject_Should_Return_Only_Selected_Properties()
        {
            var shaper = new DataShaper();
            const string fields = "Id,Name";
            IDictionary<string,object?> expandoObject = shaper.ShapeObject(_dummyModel,fields);
            
            Assert.Equal(fields.Split(',').Length, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
        }
        
        [Fact]
        public void ShapeObject_Should_Ignore_Case_Selected_Properties()
        {
            var shaper = new DataShaper();
            const string fields = "Id,nAMe";
            IDictionary<string,object?> expandoObject = shaper.ShapeObject(_dummyModel,fields);
            
            Assert.Equal(fields.Split(',').Length, expandoObject.Count);
            Assert.Contains("Id",expandoObject.Keys);
            Assert.Contains("Name",expandoObject.Keys);
        }
        
        [Fact]
        public void ShapeObject_Should_Return_Correct_Values_For_All_Properties()
        {
            var shaper = new DataShaper();
            IDictionary<string,object?> expandoObject = shaper.ShapeObject(_dummyModel,null);
            
            Assert.Equal(4,expandoObject.Count);
            Assert.Equal(_dummyModel.Id, expandoObject["Id"]);
            Assert.Equal(_dummyModel.Name, expandoObject["Name"]);
            Assert.Equal(_dummyModel.IsDeleted, expandoObject["IsDeleted"]);
            Assert.Equal(_dummyModel.CreatedAt, expandoObject["CreatedAt"]);
        }
        
        [Fact]
        public void ShapeObject_Should_Return_Correct_Values_For_Selected_Properties()
        {
            var shaper = new DataShaper();
            const string fields = "Id,Name,IsDeleted";
            IDictionary<string,object?> expandoObject = shaper.ShapeObject(_dummyModel,"id,name,isDeleted");
            
            Assert.Equal(fields.Split(',').Length, expandoObject.Count);
            Assert.Equal(_dummyModel.Id, expandoObject["Id"]);
            Assert.Equal(_dummyModel.Name, expandoObject["Name"]);
            Assert.Equal(_dummyModel.IsDeleted, expandoObject["IsDeleted"]);
        }
        
        [Fact] 
        public void ShapeData_Should_Throw_If_Property_Does_Not_Exist()
        {
            var shaper = new DataShaper();
            
            foreach (var dummyModel in _dummyModels)
            {
                Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(dummyModel,"NonExistingProperty"));
            }
        }
        
        [Fact]
        public void ShapeData_Should_Throw_For_Private_Properties()
        {
            var shaper = new DataShaper();
            foreach (var dummyModel in _dummyModels)
            {
                Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(dummyModel,"PrivateField"));
            }
        }
        
        [Fact]
        public void ShapeData_Should_Throw_For_Protected_Properties()
        {
            var shaper = new DataShaper();
            
            foreach (var dummyModel in _dummyModels)
            {
                Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(dummyModel,"ProtectedField"));
            }
        }
        
        [Fact]
        public void ShapeData_Should_Throw_For_Internal_Properties()
        {
            var shaper = new DataShaper();
            foreach (var dummyModel in _dummyModels)
            {
                Assert.Throws<PropertyNotFoundException>(() => shaper.ShapeObject(dummyModel,"InternalField"));
            }
        }
        
        [Fact]
        public void ShapeData_Should_Return_All_Properties_If_Fields_Param_Is_Null()
        {
            var shaper = new DataShaper();
            IEnumerable<IDictionary<string, object?>> expandoObjects =
                _dummyModels.Select(x => shaper.ShapeObject(x, null)).ToList();

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
            var shaper = new DataShaper();
            IEnumerable<IDictionary<string, object?>> expandoObjects =
                _dummyModels.Select(x => shaper.ShapeObject(x, string.Empty)).ToList();

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
            var shaper = new DataShaper();
            const string fields = "Id,Name";
            IEnumerable<IDictionary<string, object?>> expandoObjects =
                _dummyModels.Select(x => shaper.ShapeObject(x, fields));

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
            var shaper = new DataShaper();
            const string fields = "iD,nAMe";
            IEnumerable<IDictionary<string, object?>> expandoObjects =
                _dummyModels.Select(x => shaper.ShapeObject(x, fields)).ToList();
            
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
            var shaper = new DataShaper();
            IEnumerable<IDictionary<string, object?>> expandoObjects =
                _dummyModels.Select(x => shaper.ShapeObject(x, null)).ToList();
            
            foreach (var expandoObject in expandoObjects)
            {
                var dummyModel = _dummyModels.FirstOrDefault(x => x.Id == Guid.Parse(expandoObject["Id"]?.ToString()!));
                Assert.Equal(4,expandoObject.Count);
                Assert.Equal(dummyModel!.Id, expandoObject["Id"]);
                Assert.Equal(dummyModel.Name, expandoObject["Name"]);
                Assert.Equal(dummyModel.IsDeleted, expandoObject["IsDeleted"]);
                Assert.Equal(dummyModel.CreatedAt, expandoObject["CreatedAt"]);
            }
            Assert.Equal(expandoObjects.Count(), _dummyModels.Count);
        }
        
        [Fact]
        public void ShapeData_Should_Return_Correct_Values_For_Selected_Properties()
        {
            var shaper = new DataShaper();
            const string fields = "Id,Name,IsDeleted";
            IEnumerable<IDictionary<string, object?>> expandoObjects =
                _dummyModels.Select(x => shaper.ShapeObject(x, fields)).ToList();
            
            foreach (var expandoObject in expandoObjects)
            {
                var dummyModel = _dummyModels.FirstOrDefault(x => x.Id == Guid.Parse(expandoObject["Id"]?.ToString()!));
                Assert.Equal(fields.Split(',').Length,expandoObject.Count);
                Assert.Equal(dummyModel!.Id, expandoObject["Id"]);
                Assert.Equal(dummyModel.Name, expandoObject["Name"]);
                Assert.Equal(dummyModel.IsDeleted, expandoObject["IsDeleted"]);
            }
            Assert.Equal(expandoObjects.Count(), _dummyModels.Count);
        }
    }
}