namespace Dappi.Core.Tests.Pluralizer;

public class IrregularWordsTestData : TheoryData<string, string>
{
    public IrregularWordsTestData()
    {
        Add("person", "people");
        Add("man", "men");
        Add("woman", "women");
        Add("child", "children");
        Add("tooth", "teeth");
        Add("foot", "feet");
        Add("mouse", "mice");
        Add("goose", "geese");
        Add("ox", "oxen");
        Add("louse", "lice");
        Add("die", "dice");
        Add("index", "indices");
        Add("appendix", "appendices");
        Add("cactus", "cacti");
        Add("focus", "foci");
        Add("fungus", "fungi");
        Add("nucleus", "nuclei");
        Add("radius", "radii");
        Add("stimulus", "stimuli");
        Add("analysis", "analyses");
        Add("thesis", "theses");
        Add("crisis", "crises");
        Add("phenomenon", "phenomena");
        Add("criterion", "criteria");
        Add("datum", "data");
    }
}

public class UncountablesTestData : TheoryData<string>
{
    public UncountablesTestData()
    {
        Add("sheep");
        Add("fish");
        Add("deer");
        Add("series");
        Add("species");
        Add("money");
        Add("rice");
        Add("information");
        Add("equipment");
        Add("knowledge");
        Add("traffic");
        Add("baggage");
        Add("furniture");
        Add("advice");
    }
}

public class F_ExceptionsTestData : TheoryData<string, string>
{
    public F_ExceptionsTestData()
    {
        Add("roof", "roofs");
        Add("belief", "beliefs");
        Add("chef", "chefs");
        Add("chief", "chiefs");
        Add("proof", "proofs");
        Add("safe", "safes");
    }
}

public class F_FE_VES_TestData : TheoryData<string, string>
{
    public F_FE_VES_TestData()
    {
        Add("Knife", "Knives");
        Add("Leaf", "Leaves");
        Add("Calf", "Calves");
    }
}

public class O_ExceptionsTestData : TheoryData<string, string>
{
    public O_ExceptionsTestData()
    {
        Add("hero", "heroes");
        Add("echo", "echoes");
        Add("potato", "potatoes");
        Add("tomato", "tomatoes");
        Add("torpedo", "torpedoes");
        Add("veto", "vetoes");
    }
}

public class Y_IES_TestData : TheoryData<string, string>
{
    public Y_IES_TestData()
    {
        Add("Category", "Categories");
        Add("Baby", "Babies");
        Add("Factory", "Factories");
        Add("Story", "Stories");
    }
}

public class Y_S_TestData : TheoryData<string, string>
{
    public Y_S_TestData()
    {
        Add("Boy", "Boys");
        Add("Toy", "Toys");
        Add("Day", "Days");
    }
}

//Wors ending with S , X , Z , CH , SH
public class ES_TestData : TheoryData<string, string>
{
    public ES_TestData()
    {
        Add("Branch", "Branches");
        Add("Beach", "Beaches");
        Add("Bus", "Buses");
        Add("Campus", "Campuses");
        Add("Wish", "Wishes");
        Add("Garnish", "Garnishes");
        Add("Glass", "Glasses");
        Add("Class", "Classes");
        Add("Box", "Boxes");
        Add("Tax", "Taxes");
        Add("Quiz", "Quizes");
    }
}

public class IS_ES_TestData : TheoryData<string, string>
{
    public IS_ES_TestData()
    {
        Add("Thesis", "Theses");
        Add("Axis", "Axes");
        Add("Crisis", "Crises");
    }
}

public class O_TestData : TheoryData<string, string>
{
    public O_TestData()
    {
        Add("Video", "Videos");
        Add("Audio", "Audios");
        Add("Radio", "Radios");
        Add("Photo", "Photos");
    }
}

public class S_TestData : TheoryData<string, string>
{
    public S_TestData()
    {
        Add("Book", "Books");
        Add("Number", "Numbers");
        Add("Car", "Cars");
        Add("House", "Houses");
    }
}

public class Maintain_Case_TestData : TheoryData<string, string>
{
    public Maintain_Case_TestData()
    {
        Add("PERSON", "PEOPLE");
        Add("WISH", "WISHES");
        Add("WOLF", "WOLVES");
    }
}