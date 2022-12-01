// Copyright (c) Umbraco.
// See LICENSE for more details.

using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Models.Blocks;

namespace Umbraco.Cms.Core.PropertyEditors.ValueConverters;

public class MultiUrlPickerWithSettingsValueConverter : PropertyValueConverterBase
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IProfilingLogger _proflog;
    private readonly IPublishedSnapshotAccessor _publishedSnapshotAccessor;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly BlockEditorConverter _blockEditorConverter;

    public MultiUrlPickerWithSettingsValueConverter(
        IPublishedSnapshotAccessor publishedSnapshotAccessor,
        IProfilingLogger proflog,
        IJsonSerializer jsonSerializer,
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedUrlProvider publishedUrlProvider,
        BlockEditorConverter blockEditorConverter)
    {
        _publishedSnapshotAccessor = publishedSnapshotAccessor ??
                                     throw new ArgumentNullException(nameof(publishedSnapshotAccessor));
        _proflog = proflog ?? throw new ArgumentNullException(nameof(proflog));
        _jsonSerializer = jsonSerializer;
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedUrlProvider = publishedUrlProvider;
        _blockEditorConverter = blockEditorConverter;
    }

    public override bool IsConverter(IPublishedPropertyType propertyType) =>
        propertyType.EditorAlias == "MultiUrlPickerWithSettings";

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) =>
        propertyType.DataType.ConfigurationAs<MultiUrlPickerWithSettingsConfiguration>()!.MaxNumber == 1
            ? typeof(LinkWithSettings)
            : typeof(IEnumerable<LinkWithSettings>);

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
}

public class LinkWithSettings : Link
{
    public IPublishedElement? Settings { get; set; }
}
