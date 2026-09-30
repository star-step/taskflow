using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Tests.Api;

public sealed class StrictEnumBinderProviderTests
{
    private static IModelBinder? BinderFor(Type type, BindingSource? source)
    {
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(type);
        return new StrictEnumModelBinderProvider().GetBinder(new Context(metadata, new BindingInfo { BindingSource = source }));
    }

    [Theory]
    [InlineData("Query")]
    [InlineData("Path")]
    [InlineData("ModelBinding")]
    [InlineData(null)]
    public void BindsEnums_FromQueryRouteOrUnspecified(string? source)
    {
        var bindingSource = source switch
        {
            "Query" => BindingSource.Query,
            "Path" => BindingSource.Path,
            "ModelBinding" => BindingSource.ModelBinding,
            _ => null
        };
        Assert.NotNull(BinderFor(typeof(TaskItemStatus), bindingSource));
        Assert.NotNull(BinderFor(typeof(TaskPriority?), bindingSource));
    }

    [Fact]
    public void LeavesBodyHeaderAndServicesToTheirOwnBinders()
    {
        Assert.Null(BinderFor(typeof(TaskItemStatus), BindingSource.Body));
        Assert.Null(BinderFor(typeof(TaskItemStatus), BindingSource.Header));
        Assert.Null(BinderFor(typeof(TaskItemStatus), BindingSource.Services));
    }

    [Fact]
    public void IgnoresNonEnums() => Assert.Null(BinderFor(typeof(string), BindingSource.Query));

    private sealed class Context(ModelMetadata metadata, BindingInfo bindingInfo) : ModelBinderProviderContext
    {
        public override BindingInfo BindingInfo => bindingInfo;
        public override ModelMetadata Metadata => metadata;
        public override IModelMetadataProvider MetadataProvider { get; } = new EmptyModelMetadataProvider();
        public override IModelBinder CreateBinder(ModelMetadata metadata) => throw new NotSupportedException();
    }
}
