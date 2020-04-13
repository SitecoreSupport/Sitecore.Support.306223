using System;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Mvc.Extensions;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.XA.Foundation.Grid;
using Sitecore.XA.Foundation.Presentation.Layout;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;

namespace Sitecore.Support.XA.Feature.Composites.EventHandlers
{
    [UsedImplicitly]
    public class CompositeItemHandler
    {
        public IGridContext GridContext { get; set; }

        public CompositeItemHandler(IGridContext gridContext)
        {
            GridContext = gridContext;
        }

        [UsedImplicitly]
        public void OnItemCopied(object sender, EventArgs args)
        {
            var item = (Item)Event.ExtractParameter(args, 1);
            HandleCompositeItem(item);
        }

        [UsedImplicitly]
        protected void OnItemAdded(object sender, EventArgs args)
        {
            var item = (Item)Event.ExtractParameter(args, 0);
            HandleCompositeItem(item);
        }

        protected virtual void HandleCompositeItem(Item item)
        {
            if (item.InheritsFrom(Sitecore.XA.Feature.Composites.Templates.CompositeGroup.ID))
            {
                item.ChildrenInheritingFrom(Sitecore.XA.Feature.Composites.Templates.CompositeSection.ID).Each(ProcessCompositeItem);
            }
            else if (item.InheritsFrom(Sitecore.XA.Feature.Composites.Templates.CompositeSection.ID))
            {
                ProcessCompositeItem(item);
            }
        }

        protected virtual void ProcessCompositeItem(Item item)
        {
            var model = new LayoutModel(item);
            foreach (var device in model.Devices.DevicesCollection)
            {
                var deviceItem = item.Database.GetItem(device.DeviceId);
                Item gridDefinitionItem = GridContext.GetGridDefinitionItem(item, deviceItem);
                string defaultParameters = null;
                if (gridDefinitionItem != null)
                {
                    defaultParameters = gridDefinitionItem[Sitecore.XA.Foundation.Grid.Templates.GridDefinition.Fields.DefaultGridParameters];
                }
                foreach (var rendering in device.Renderings.RenderingsCollection)
                {
                    rendering.UniqueId = new ID(Guid.NewGuid());
                    if (defaultParameters != null)
                    {
                        rendering.Parameters[Sitecore.XA.Foundation.Grid.Constants.GridParametersFieldName] = defaultParameters;
                    }
                }
            }
            SetLayoutValue(item, model);
        }

        protected void SetLayoutValue(Item item, LayoutModel model)
        {
            var layoutField = new LayoutField(item);
            item.Editing.BeginEdit();
            var layoutFieldValue = model.ToString();
            if (ShouldSaveSharedLayout(item))
            {
                item.Fields[FieldIDs.LayoutField].Value = layoutFieldValue;
            }
            else
            {
                layoutField.Value = layoutFieldValue;
            }
            item.Editing.EndEdit();
        }

        protected virtual bool ShouldSaveSharedLayout(Item item)
        {
            var editAllVersions = Registry.GetString(ExperienceEditor.Constants.RegistryKeys.EditAllVersions);
            var finalLayoutValue = item.Fields[FieldIDs.FinalLayoutField].Value;
            return editAllVersions.Equals("on", StringComparison.OrdinalIgnoreCase) || finalLayoutValue.IsNullOrEmpty();
        }
    }
}