using PridrIntergrationTest.Helpers;

namespace PridrIntergrationTest.Setup;

[CollectionDefinition("ContainerTests", DisableParallelization = true)]
public class ContainterTestCollection: ICollectionFixture<SharedContainerSetup>
{
    
}