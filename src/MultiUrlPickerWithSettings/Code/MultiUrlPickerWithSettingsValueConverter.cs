// Copyright (c) Umbraco.
// See LICENSE for more details.

using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.DeliveryApi;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.DeliveryApi;
using static Umbraco.Cms.Core.Constants.HttpContext;

namespace Umbraco.Cms.Core.PropertyEditors.ValueConverters;

public class MultiUrlPickerWithSettingsValueConverter : PropertyValueConverterBase, IDeliveryApiPropertyValueConverter
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IProfilingLogger _proflog;
    private readonly IPublishedSnapshotAccessor _publishedSnapshotAccessor;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly BlockEditorConverter _blockEditorConverter;
    private readonly IApiContentNameProvider _apiContentNameProvider;
    private readonly IApiMediaUrlProvider _apiMediaUrlProvider;
    private readonly IApiContentRouteBuilder _apiContentRouteBuilder;
    private readonly IApiElementBuilder _apiElementBuilder;

    public MultiUrlPickerWithSettingsValueConverter(
        IPublishedSnapshotAccessor publishedSnapshotAccessor,
        IProfilingLogger proflog,
        IJsonSerializer jsonSerializer,
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedUrlProvider publishedUrlProvider,
        BlockEditorConverter blockEditorConverter,
        IApiContentNameProvider apiContentNameProvider,
        IApiMediaUrlProvider apiMediaUrlProvider,
        IApiContentRouteBuilder apiContentRouteBuilder,
        IApiElementBuilder apiElementBuilder)
    {
        _publishedSnapshotAccessor = publishedSnapshotAccessor ??
                                     throw new ArgumentNullException(nameof(publishedSnapshotAccessor));
        _proflog = proflog ?? throw new ArgumentNullException(nameof(proflog));
        _jsonSerializer = jsonSerializer;
        _publishedUrlProvider = publishedUrlProvider;
        _blockEditorConverter = blockEditorConverter;
        _apiContentNameProvider = apiContentNameProvider;
        _apiMediaUrlProvider = apiMediaUrlProvider;
        _apiContentRouteBuilder = apiContentRouteBuilder;
        _apiElementBuilder = apiElementBuilder;
    }

    public override bool IsConverter(IPublishedPropertyType propertyType) =>
        propertyType.EditorAlias == "MultiUrlPickerWithSettings";

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) =>
           IsSingleUrlPicker(propertyType)
               ? typeof(Link)
               : typeof(IEnumerable<Link>);

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) =>
        PropertyCacheLevel.Snapshot;

    public override bool? IsValue(object? value, PropertyValueLevel level) =>
        value is not null && value.ToString() != "[]";

    public override object ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object? source, bool preview) => source?.ToString()!;

    public override object? ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
    {
        using (_proflog.DebugDuration<MultiUrlPickerWithSettingsValueConverter>(
                   $"ConvertPropertyToLinks ({propertyType.DataType.Id})"))
        {
            var maxNumber = propertyType.DataType.ConfigurationAs<MultiUrlPickerWithSettingsConfiguration>()!.MaxNumber;

            if (string.IsNullOrWhiteSpace(inter?.ToString()))
            {
                return maxNumber == 1 ? null : Enumerable.Empty<LinkWithSettings>();
            }

            var links = new List<LinkWithSettings>();
            IEnumerable<MultiUrlPickerWithSettingsValueEditor.LinkDto>? dtos =
                _jsonSerializer.Deserialize<IEnumerable<MultiUrlPickerWithSettingsValueEditor.LinkDto>>(inter.ToString()!);
            IPublishedSnapshot publishedSnapshot = _publishedSnapshotAccessor.GetRequiredPublishedSnapshot();
            if (dtos is null)
            {
                return links;
            }

            foreach (MultiUrlPickerWithSettingsValueEditor.LinkDto dto in dtos)
            {
                LinkType type = LinkType.External;
                var url = dto.Url;

                if (dto.Udi is not null)
                {
                    type = dto.Udi.EntityType == Constants.UdiEntityType.Media
                        ? LinkType.Media
                        : LinkType.Content;

                    IPublishedContent? content = type == LinkType.Media
                        ? publishedSnapshot.Media?.GetById(preview, dto.Udi.Guid)
                        : publishedSnapshot.Content?.GetById(preview, dto.Udi.Guid);

                    if (content == null || content.ContentType.ItemType == PublishedItemType.Element)
                    {
                        continue;
                    }

                    url = content.Url(_publishedUrlProvider);
                }



                links.Add(
                    new LinkWithSettings
                    {
                        Name = dto.Name,
                        Target = dto.Target,
                        Type = type,
                        Udi = dto.Udi,
                        Url = url + dto.QueryString,
                        Settings = GetSettingsAsPublishedElement(dto),
                    });
            }

            if (maxNumber == 1)
            {
                return links.FirstOrDefault();
            }

            if (maxNumber > 0)
            {
                return links.Take(maxNumber);
            }

            return links;
        }
    }

    private IPublishedElement? GetSettingsAsPublishedElement(MultiUrlPickerWithSettingsValueEditor.LinkDto dto)
    {
        var settingsBlockData = JsonConvert.DeserializeObject<BlockItemData>(dto.Settings);

        if(settingsBlockData != null)
        {
            return _blockEditorConverter.ConvertToElement(settingsBlockData, PropertyCacheLevel.Element, false);
        }
        else
        {
            return null;
        }
    }

    public PropertyCacheLevel GetDeliveryApiPropertyCacheLevel(IPublishedPropertyType propertyType) => PropertyCacheLevel.Elements;

    public Type GetDeliveryApiPropertyValueType(IPublishedPropertyType propertyType) => typeof(IEnumerable<ApiLink>);

    public object? ConvertIntermediateToDeliveryApiObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview, bool expanding)
    {
        IEnumerable<ApiLink> DefaultValue() => Array.Empty<ApiLink>();

        if (inter is not string value || value.IsNullOrWhiteSpace())
        {
            return DefaultValue();
        }

        MultiUrlPickerWithSettingsValueEditor.LinkDto[]? dtos = ParseLinkDtos(value)?.ToArray();
        if (dtos == null || dtos.Any() == false)
        {
            return DefaultValue();
        }

        IPublishedSnapshot publishedSnapshot = _publishedSnapshotAccessor.GetRequiredPublishedSnapshot();

        ApiLinkWithSettings ? ToLink(MultiUrlPickerWithSettingsValueEditor.LinkDto item)
        {
            IApiElement? settings = null;
            var contentItem = GetSettingsAsPublishedElement(item);
            if (contentItem != null)
            {
                settings = _apiElementBuilder.Build(contentItem);
            }

            switch (item.Udi?.EntityType)
            {
                case Constants.UdiEntityType.Document:
                    IPublishedContent? content = publishedSnapshot.Content?.GetById(item.Udi.Guid);
                    IApiContentRoute? route = content != null
                        ? _apiContentRouteBuilder.Build(content)
                        : null;
                    return content == null || route == null
                        ? null
                        : ApiLinkWithSettings.Content(
                            item.Name.IfNullOrWhiteSpace(_apiContentNameProvider.GetName(content)),
                            item.Target,
                            content.Key,
                            content.ContentType.Alias,
                            route,
                            settings);
                case Constants.UdiEntityType.Media:
                    IPublishedContent? media = publishedSnapshot.Media?.GetById(item.Udi.Guid);
                    return media == null
                        ? null
                        : ApiLinkWithSettings.Media(
                            item.Name.IfNullOrWhiteSpace(_apiContentNameProvider.GetName(media)),
                            _apiMediaUrlProvider.GetUrl(media),
                            item.Target,
                            media.Key,
                            media.ContentType.Alias,
                            settings);
                default:
                    return ApiLinkWithSettings.External(item.Name, $"{item.Url}{item.QueryString}", item.Target, settings);
            }
        }

        return dtos.Select(ToLink).WhereNotNull().ToArray();
    }

    private static bool IsSingleUrlPicker(IPublishedPropertyType propertyType)
        => propertyType.DataType.ConfigurationAs<MultiUrlPickerWithSettingsConfiguration>()!.MaxNumber == 1;

    private IEnumerable<MultiUrlPickerWithSettingsValueEditor.LinkDto>? ParseLinkDtos(string inter)
        => inter.DetectIsJson() ? _jsonSerializer.Deserialize<IEnumerable<MultiUrlPickerWithSettingsValueEditor.LinkDto>>(inter) : null;
}

public class LinkWithSettings : Link
{
    public IPublishedElement? Settings { get; set; }
}
