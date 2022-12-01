using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;

namespace MultiUrlPickerWithSettings
{

    public class Composer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<ManifestFilter>();
        }
    }

    public class ManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            manifests.Add(new PackageManifest()
            {
                PackageName = "MultiUrlPickerWithSettings",
                Scripts = new[]
                {
                    "/App_Plugins/MultiUrlPickerWithSettings/blockpickerconfiguration/blocklist.blockconfiguration.controller.js",
                    "/App_Plugins/MultiUrlPickerWithSettings/multiurlpicker.controller.js",
                    "/App_Plugins/MultiUrlPickerWithSettings/nodePreview/umbnodepreview.directive.js"
                }
            });
        }
    }
}
