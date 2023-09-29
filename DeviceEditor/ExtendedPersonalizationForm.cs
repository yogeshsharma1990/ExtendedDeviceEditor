using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Extensions.XElementExtensions;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Pipelines.RenderRulePlaceholder;
using Sitecore.Resources;
using Sitecore.Rules;
using Sitecore.Shell.Applications.Dialogs.Personalize;
using Sitecore.Shell.Applications.Dialogs.RulesEditor;
using Sitecore.Shell.Applications.Rules;
using Sitecore.Shell.Controls;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml.Linq;
using Sitecore;
using Sitecore.StringExtensions;

namespace Sitecore.ExtendedLayoutEditor.DeviceEditor
{
    public class ExtendedPersonalizationForm : DialogForm
    {
        /// <summary>
        /// The name of the placeholder positioned after the action.
        /// </summary>
        public const string AfterActionPlaceholderName = "afterAction";
        /// <summary>The default condition description</summary>
        protected readonly string ConditionDescriptionDefault;
        /// <summary>The default condition id string</summary>
        protected static readonly string DefaultConditionId = Analytics.AnalyticsIds.DefaultPersonalizationRuleCondition.ToString();
        /// <summary>The default condition name</summary>
        protected readonly string ConditionNameDefault;
        /// <summary>The component personalization</summary>
        protected Checkbox ComponentPersonalization;
        /// <summary>The personalization tracking checkbox</summary>
        protected Checkbox PersonalizationTracking;
        /// <summary>The rules container.</summary>
        protected Scrollbox RulesContainer;
        /// <summary>The hide rendering action id.</summary>
        private string HideRenderingActionId = RuleIds.HideRenderingActionId.ToString();
        /// <summary>The set datasource action id.</summary>
        private string SetDatasourceActionId = RuleIds.SetDatasourceActionId.ToString();
        /// <summary>The set rendering action id.</summary>
        private string SetRenderingActionId = RuleIds.SetRenderingActionId.ToString();
        /// <summary>The new condition name.</summary>
        private readonly string newConditionName = Translate.Text("Specify name...");
        /// <summary>The default condition</summary>
        private readonly XElement defaultCondition;

        /// <summary>Gets the context item.</summary>
        /// <value>The context item.</value>
        public Item ContextItem
        {
            get
            {
                ItemUri uri = ItemUri.Parse(this.ContextItemUri);
                return uri != (ItemUri)null ? Database.GetItem(uri) : (Item)null;
            }
        }

        /// <summary>Gets or sets the context item URI.</summary>
        /// <value>The context item URI.</value>
        public string ContextItemUri
        {
            get => this.ServerProperties[nameof(ContextItemUri)] as string;
            set => this.ServerProperties[nameof(ContextItemUri)] = (object)value;
        }

        /// <summary>Gets or sets the device id.</summary>
        /// <value>The device id.</value>
        public string DeviceId
        {
            get => Assert.ResultNotNull<string>(this.ServerProperties["deviceId"] as string);
            set
            {
                Assert.IsNotNullOrEmpty(value, nameof(value));
                this.ServerProperties["deviceId"] = (object)value;
            }
        }

        /// <summary>Gets the layout.</summary>
        /// <value>The layout.</value>
        public string Layout => Assert.ResultNotNull<string>(WebUtil.GetSessionString(this.SessionHandle));

        /// <summary>Gets the layout defition.</summary>
        /// <value>The layout defition.</value>
        public LayoutDefinition LayoutDefition => LayoutDefinition.Parse(this.Layout);

        /// <summary>Gets or sets the rendering reference unique id.</summary>
        /// <value>The  id.</value>
        public string ReferenceId
        {
            get => Assert.ResultNotNull<string>(this.ServerProperties["referenceId"] as string);
            set
            {
                Assert.IsNotNullOrEmpty(value, nameof(value));
                this.ServerProperties["referenceId"] = (object)value;
            }
        }

        /// <summary>Gets the rendering defition.</summary>
        /// <value>The rendering defition.</value>
        public RenderingDefinition RenderingDefition => Assert.ResultNotNull<RenderingDefinition>(this.LayoutDefition.GetDevice(this.DeviceId).GetRenderingByUniqueId(this.ReferenceId));

        /// <summary>Gets or sets RulesSet.</summary>
        /// <value>The rules set.</value>
        public XElement RulesSet
        {
            get
            {
                string serverProperty = this.ServerProperties["ruleSet"] as string;
                return !string.IsNullOrEmpty(serverProperty) ? XElement.Parse(serverProperty) : new XElement((XName)"ruleset", (object)this.defaultCondition);
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.ServerProperties["ruleSet"] = (object)value.ToString();
            }
        }

        /// <summary>Gets or sets the session handle.</summary>
        /// <value>The session handle.</value>
        public string SessionHandle
        {
            get => Assert.ResultNotNull<string>(this.ServerProperties[nameof(SessionHandle)] as string);
            set
            {
                Assert.IsNotNullOrEmpty(value, "session handle");
                this.ServerProperties[nameof(SessionHandle)] = (object)value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.WebEdit.Dialogs.Personalization.ExtendedPersonalizationForm" /> class.
        /// </summary>
        public ExtendedPersonalizationForm()
        {
            this.ConditionDescriptionDefault = Translate.Text("If none of the other conditions are true, the default condition is used.");
            this.ConditionNameDefault = Translate.Text("Default");
            this.newConditionName = Translate.Text("Specify name...");
            this.defaultCondition = XElement.Parse(string.Format("<rule uid=\"{0}\" name=\"{1}\"><conditions><condition id=\"{2}\" uid=\"{3}\" /></conditions><actions /></rule>", (object)ExtendedPersonalizationForm.DefaultConditionId, (object)this.ConditionNameDefault, (object)RuleIds.TrueConditionId, (object)ID.NewID.ToShortID()));
        }

        /// <summary>Handles the toggle component click.</summary>
        protected void ComponentPersonalizationClick()
        {
            if (!this.ComponentPersonalization.Checked && this.PersonalizeComponentActionExists())
                Context.ClientPage.Start((object)this, "ShowConfirm", new NameValueCollection());
            else
                SheerResponse.Eval("scTogglePersonalizeComponentSection()");
        }

        /// <summary>Deletes the ruel click.</summary>
        /// <param name="id">The id.</param>
        protected void DeleteRuleClick(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            string uId = ID.Decode(id).ToString();
            XElement rulesSet = this.RulesSet;
            XElement xelement = rulesSet.Elements((XName)"rule").Where<XElement>((Func<XElement, bool>)(node => node.GetAttributeValue("uid") == uId)).FirstOrDefault<XElement>();
            if (xelement == null)
                return;
            xelement.Remove();
            this.RulesSet = rulesSet;
            SheerResponse.Remove(id + "data");
            SheerResponse.Remove(id);
            if (this.HasRules())
                return;
            this.SetPersonalizationTrackingCheckbox();
        }

        /// <summary>Edits the condition.</summary>
        /// <param name="args">The arguments.</param>
        protected void EditCondition(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            if (string.IsNullOrEmpty(args.Parameters["id"]))
            {
                SheerResponse.Alert("Please select a rule");
            }
            else
            {
                string conditionId = ID.Decode(args.Parameters["id"]).ToString();
                if (!args.IsPostBack)
                {
                    RulesEditorOptions rulesEditorOptions = new RulesEditorOptions()
                    {
                        IncludeCommon = true,
                        RulesPath = "/sitecore/system/settings/Rules/Conditional Renderings",
                        AllowMultiple = false
                    };
                    XElement xelement = this.RulesSet.Elements((XName)"rule").Where<XElement>((Func<XElement, bool>)(node => node.GetAttributeValue("uid") == conditionId)).FirstOrDefault<XElement>();
                    if (xelement != null)
                        rulesEditorOptions.Value = "<ruleset>" + (object)xelement + "</ruleset>";
                    rulesEditorOptions.HideActions = true;
                    SheerResponse.ShowModalDialog(rulesEditorOptions.ToUrlString().ToString(), "580px", "712px", string.Empty, true);
                    args.WaitForPostBack();
                }
                else
                {
                    if (!args.HasResult)
                        return;
                    XElement rule = XElement.Parse(args.Result).Element((XName)"rule");
                    XElement rulesSet = this.RulesSet;
                    if (rule == null)
                        return;
                    XElement xelement = rulesSet.Elements((XName)"rule").Where<XElement>((Func<XElement, bool>)(node => node.GetAttributeValue("uid") == conditionId)).FirstOrDefault<XElement>();
                    if (xelement == null)
                        return;
                    xelement.ReplaceWith((object)rule);
                    this.RulesSet = rulesSet;
                    SheerResponse.SetInnerHtml(args.Parameters["id"] + "_rule", ExtendedPersonalizationForm.GetRuleConditionsHtml(rule));
                }
            }
        }

        /// <summary>Edits the rule.</summary>
        /// <param name="id">The rule.</param>
        protected void EditConditionClick(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            Context.ClientPage.Start((object)this, "EditCondition", new NameValueCollection()
            {
                [nameof(id)] = id
            });
        }

        /// <summary>Moves the condition after the specified one.</summary>
        /// <param name="id">The id.</param>
        /// <param name="targetId">The target id.</param>
        protected void MoveConditionAfter(string id, string targetId)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            Assert.ArgumentNotNull((object)targetId, nameof(targetId));
            XElement rulesSet = this.RulesSet;
            XElement ruleById1 = ExtendedPersonalizationForm.GetRuleById(rulesSet, id);
            XElement ruleById2 = ExtendedPersonalizationForm.GetRuleById(rulesSet, targetId);
            if (ruleById1 == null || ruleById2 == null)
                return;
            ruleById1.Remove();
            ruleById2.AddAfterSelf((object)ruleById1);
            this.RulesSet = rulesSet;
        }

        /// <summary>Moves the condition before the specified one.</summary>
        /// <param name="id">The id.</param>
        /// <param name="targetId">The target id.</param>
        protected void MoveConditionBefore(string id, string targetId)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            Assert.ArgumentNotNull((object)targetId, nameof(targetId));
            XElement rulesSet = this.RulesSet;
            XElement ruleById1 = ExtendedPersonalizationForm.GetRuleById(rulesSet, id);
            XElement ruleById2 = ExtendedPersonalizationForm.GetRuleById(rulesSet, targetId);
            if (ruleById1 == null || ruleById2 == null)
                return;
            ruleById1.Remove();
            ruleById2.AddBeforeSelf((object)ruleById1);
            this.RulesSet = rulesSet;
        }

        /// <summary>News the condition click.</summary>
        protected void NewConditionClick()
        {
            XElement rule = new XElement((XName)"rule");
            rule.SetAttributeValue((XName)"name", (object)this.newConditionName);
            ID newId = ID.NewID;
            rule.SetAttributeValue((XName)"uid", (object)newId);
            XElement rulesSet = this.RulesSet;
            rulesSet.AddFirst((object)rule);
            this.RulesSet = rulesSet;
            SheerResponse.Insert("non-default-container", "afterBegin", this.GetRuleSectionHtml(rule));
            SheerResponse.Eval("Sitecore.CollapsiblePanel.addNew(\"" + (object)newId.ToShortID() + "\")");
            int num = 2;
            if (this.RulesSet.Elements().Count<XElement>() != num)
                return;
            this.PersonalizationTracking.Checked = true;
            this.PersonalizationTracking.Disabled = false;
        }

        /// <summary>The on load.</summary>
        /// <param name="e">The e.</param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull((object)e, nameof(e));
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
            {
                SheerResponse.Eval("Sitecore.CollapsiblePanel.collapseMenus()");
            }
            else
            {
                PersonalizeOptions personalizeOptions = PersonalizeOptions.Parse();
                this.DeviceId = personalizeOptions.DeviceId;
                this.ReferenceId = personalizeOptions.RenderingUniqueId;
                this.SessionHandle = personalizeOptions.SessionHandle;
                this.ContextItemUri = personalizeOptions.ContextItemUri;
                XElement rules = this.RenderingDefition.Rules;
                if (rules != null)
                    this.RulesSet = rules;
                if (this.PersonalizeComponentActionExists())
                    this.ComponentPersonalization.Checked = true;
                this.RenderRules();
                this.SetPersonalizationTrackingCheckbox();
            }
        }

        /// <summary>Handles a click on the OK button.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)args, nameof(args));
            XElement rulesSet = this.RulesSet;
            if (this.PersonalizationTrackingCheckboxEnabled())
                rulesSet.SetAttributeValue((XName)"pet", (object)this.PersonalizationTracking.Checked);
            SheerResponse.SetDialogValue(rulesSet.ToString());
            base.OnOK(sender, args);
        }

        /// <summary>Rename the rule.</summary>
        /// <param name="message">The new message.</param>
        [HandleMessage("rule:rename")]
        protected void RenameRuleClick(Message message)
        {
            Assert.ArgumentNotNull((object)message, nameof(message));
            string id = message.Arguments["ruleId"];
            string str = message.Arguments["name"];
            Assert.IsNotNull((object)id, "id");
            if (string.IsNullOrEmpty(str))
                return;
            XElement rulesSet = this.RulesSet;
            XElement ruleById = ExtendedPersonalizationForm.GetRuleById(rulesSet, id);
            if (ruleById == null)
                return;
            ruleById.SetAttributeValue((XName)"name", (object)str);
            this.RulesSet = rulesSet;
        }

        /// <summary>Resets the datasource.</summary>
        /// <param name="id">The id.</param>
        protected void ResetDatasource(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            if (!this.IsComponentDisplayed(id))
                return;
            XElement rulesSet = this.RulesSet;
            XElement ruleById = ExtendedPersonalizationForm.GetRuleById(rulesSet, id);
            if (ruleById == null)
                return;
            ExtendedPersonalizationForm.RemoveAction(ruleById, this.SetDatasourceActionId);
            this.RulesSet = rulesSet;
            HtmlTextWriter writer = new HtmlTextWriter((TextWriter)new StringWriter());
            this.RenderSetDatasourceAction(ruleById, writer);
            SheerResponse.SetInnerHtml(id + "_setdatasource", writer.InnerWriter.ToString().Replace("{ID}", id));
        }

        /// <summary>Resets the rendering.</summary>
        /// <param name="id">The id.</param>
        protected void ResetRendering(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            if (!this.IsComponentDisplayed(id))
                return;
            XElement rulesSet = this.RulesSet;
            XElement ruleById = ExtendedPersonalizationForm.GetRuleById(rulesSet, id);
            if (ruleById == null)
                return;
            ExtendedPersonalizationForm.RemoveAction(ruleById, this.SetRenderingActionId);
            this.RulesSet = rulesSet;
            HtmlTextWriter writer = new HtmlTextWriter((TextWriter)new StringWriter());
            this.RenderSetRenderingAction(ruleById, writer);
            SheerResponse.SetInnerHtml(id + "_setrendering", writer.InnerWriter.ToString().Replace("{ID}", id));
        }

        /// <summary>Sets the rendering click.</summary>
        /// <param name="args">The args.</param>
        protected void SetDatasource(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            string parameter = args.Parameters["id"];
            XElement rulesSet = this.RulesSet;
            XElement ruleById = ExtendedPersonalizationForm.GetRuleById(rulesSet, parameter);
            Assert.IsNotNull((object)ruleById, "rule");
            if (!args.IsPostBack)
            {
                XElement actionById1 = ExtendedPersonalizationForm.GetActionById(ruleById, this.SetRenderingActionId);
                Item renderingItem = (Item)null;
                if (actionById1 != null && !string.IsNullOrEmpty(actionById1.GetAttributeValue("RenderingItem")))
                    renderingItem = Sitecore.Client.ContentDatabase.GetItem(actionById1.GetAttributeValue("RenderingItem"));
                else if (!string.IsNullOrEmpty(this.RenderingDefition.ItemID))
                    renderingItem = Sitecore.Client.ContentDatabase.GetItem(this.RenderingDefition.ItemID);
                if (renderingItem == null)
                {
                    SheerResponse.Alert("Item not found.");
                }
                else
                {
                    Item contextItem = this.ContextItem;
                    GetRenderingDatasourceArgs renderingDatasourceArgs = new GetRenderingDatasourceArgs(renderingItem)
                    {
                        FallbackDatasourceRoots = new List<Item>()
            {
              Sitecore.Client.ContentDatabase.GetRootItem()
            },
                        ContentLanguage = contextItem?.Language,
                        ContextItemPath = contextItem != null ? contextItem.Paths.LongID : string.Empty,
                        ShowDialogIfDatasourceSetOnRenderingItem = true
                    };
                    XElement actionById2 = ExtendedPersonalizationForm.GetActionById(ruleById, this.SetDatasourceActionId);
                    renderingDatasourceArgs.CurrentDatasource = actionById2 == null || string.IsNullOrEmpty(actionById2.GetAttributeValue("DataSource")) ? this.RenderingDefition.Datasource : actionById2.GetAttributeValue("DataSource");
                    if (string.IsNullOrEmpty(renderingDatasourceArgs.CurrentDatasource))
                        renderingDatasourceArgs.CurrentDatasource = contextItem.ID.ToString();
                    CorePipeline.Run("getRenderingDatasource", (PipelineArgs)renderingDatasourceArgs);
                    if (string.IsNullOrEmpty(renderingDatasourceArgs.DialogUrl))
                    {
                        SheerResponse.Alert("An error occurred.");
                    }
                    else
                    {
                        SheerResponse.ShowModalDialog(renderingDatasourceArgs.DialogUrl, "960px", "660px", string.Empty, true);
                        args.WaitForPostBack();
                    }
                }
            }
            else
            {
                if (!args.HasResult)
                    return;
                (ExtendedPersonalizationForm.GetActionById(ruleById, this.SetDatasourceActionId) ?? ExtendedPersonalizationForm.AddAction(ruleById, this.SetDatasourceActionId)).SetAttributeValue((XName)"DataSource", (object)args.Result);
                this.RulesSet = rulesSet;
                HtmlTextWriter writer = new HtmlTextWriter((TextWriter)new StringWriter());
                this.RenderSetDatasourceAction(ruleById, writer);
                SheerResponse.SetInnerHtml(parameter + "_setdatasource", writer.InnerWriter.ToString().Replace("{ID}", parameter));
            }
        }

        /// <summary>Sets the datasource click.</summary>
        /// <param name="id">The id.</param>
        protected void SetDatasourceClick(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            if (!this.IsComponentDisplayed(id))
                return;
            Context.ClientPage.Start((object)this, "SetDatasource", new NameValueCollection()
            {
                [nameof(id)] = id
            });
        }

        /// <summary>Edits the condition.</summary>
        /// <param name="args">The arguments.</param>
        protected void SetRendering(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            if (!args.IsPostBack)
            {
                string placeholder = this.RenderingDefition.Placeholder;
                Assert.IsNotNull((object)placeholder, "placeholder");
                string layout = this.Layout;
                GetPlaceholderRenderingsArgs placeholderRenderingsArgs = new GetPlaceholderRenderingsArgs(placeholder, layout, Sitecore.Client.ContentDatabase, ID.Parse(this.DeviceId));
                placeholderRenderingsArgs.OmitNonEditableRenderings = true;
                placeholderRenderingsArgs.Options.ShowOpenProperties = false;
                CorePipeline.Run("getPlaceholderRenderings", (PipelineArgs)placeholderRenderingsArgs);
                string dialogUrl = placeholderRenderingsArgs.DialogURL;
                if (string.IsNullOrEmpty(dialogUrl))
                {
                    SheerResponse.Alert("An error occurred.");
                }
                else
                {
                    SheerResponse.ShowModalDialog(dialogUrl, "720px", "470px", string.Empty, true);
                    args.WaitForPostBack();
                }
            }
            else
            {
                if (!args.HasResult)
                    return;
                string result;
                if (args.Result.IndexOf(',') >= 0)
                    result = args.Result.Split(',')[0];
                else
                    result = args.Result;
                XElement rulesSet = this.RulesSet;
                string parameter = args.Parameters["id"];
                XElement ruleById = ExtendedPersonalizationForm.GetRuleById(rulesSet, parameter);
                Assert.IsNotNull((object)ruleById, "rule");
                (ExtendedPersonalizationForm.GetActionById(ruleById, this.SetRenderingActionId) ?? ExtendedPersonalizationForm.AddAction(ruleById, this.SetRenderingActionId)).SetAttributeValue((XName)"RenderingItem", (object)ShortID.DecodeID(result));
                this.RulesSet = rulesSet;
                HtmlTextWriter writer = new HtmlTextWriter((TextWriter)new StringWriter());
                this.RenderSetRenderingAction(ruleById, writer);
                SheerResponse.SetInnerHtml(parameter + "_setrendering", writer.InnerWriter.ToString().Replace("{ID}", parameter));
            }
        }

        /// <summary>Sets the rendering click.</summary>
        /// <param name="id">The id.</param>
        protected void SetRenderingClick(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            if (!this.IsComponentDisplayed(id))
                return;
            Context.ClientPage.Start((object)this, "SetRendering", new NameValueCollection()
            {
                [nameof(id)] = id
            });
        }

        /// <summary>Shows the confirm.</summary>
        /// <param name="args">The arguments.</param>
        protected void ShowConfirm(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            if (args.IsPostBack)
            {
                if (args.HasResult && args.Result != "no")
                {
                    SheerResponse.Eval("scTogglePersonalizeComponentSection()");
                    XElement rulesSet = this.RulesSet;
                    foreach (XElement element in rulesSet.Elements((XName)"rule"))
                    {
                        XElement actionById = ExtendedPersonalizationForm.GetActionById(element, this.SetRenderingActionId);
                        if (actionById != null)
                        {
                            actionById.Remove();
                            HtmlTextWriter writer = new HtmlTextWriter((TextWriter)new StringWriter());
                            this.RenderSetRenderingAction(element, writer);
                            ShortID shortId = ShortID.Parse(element.GetAttributeValue("uid"));
                            Assert.IsNotNull((object)shortId, "ruleId");
                            SheerResponse.SetInnerHtml(shortId.ToString() + "_setrendering", writer.InnerWriter.ToString().Replace("{ID}", shortId.ToString()));
                        }
                    }
                    this.RulesSet = rulesSet;
                }
                else
                    this.ComponentPersonalization.Checked = true;
            }
            else
            {
                SheerResponse.Confirm("Personalize component settings will be removed. Are you sure you want to continue?");
                args.WaitForPostBack();
            }
        }

        /// <summary>Switches the rendering click.</summary>
        /// <param name="id">The id.</param>
        protected void SwitchRenderingClick(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            XElement rulesSet = this.RulesSet;
            XElement ruleById = ExtendedPersonalizationForm.GetRuleById(rulesSet, id);
            if (ruleById == null)
                return;
            if (!this.IsComponentDisplayed(ruleById))
                ExtendedPersonalizationForm.RemoveAction(ruleById, this.HideRenderingActionId);
            else
                ExtendedPersonalizationForm.AddAction(ruleById, this.HideRenderingActionId);
            this.RulesSet = rulesSet;
        }

        /// <summary>Adds the action.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="id">The id.</param>
        /// <returns>The action.</returns>
        private static XElement AddAction(XElement rule, string id)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)id, nameof(id));
            XElement xelement1 = new XElement((XName)"action", new object[2]
            {
        (object) new XAttribute((XName) nameof (id), (object) id),
        (object) new XAttribute((XName) "uid", (object) ID.NewID.ToShortID())
            });
            XElement xelement2 = rule.Element((XName)"actions");
            if (xelement2 == null)
                rule.Add((object)new XElement((XName)"actions", (object)xelement1));
            else
                xelement2.Add((object)xelement1);
            return xelement1;
        }

        /// <summary>Gets the action by id.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="id">The id.</param>
        /// <returns>The action by id.</returns>
        private static XElement GetActionById(XElement rule, string id)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)id, nameof(id));
            XElement xelement = rule.Element((XName)"actions");
            return xelement == null ? (XElement)null : xelement.Elements((XName)"action").FirstOrDefault<XElement>((Func<XElement, bool>)(action => action.GetAttributeValue(nameof(id)) == id));
        }

        /// <summary>Gets the rule by id.</summary>
        /// <param name="ruleSet">The rule set.</param>
        /// <param name="id">The id.</param>
        /// <returns>The rule by id.</returns>
        private static XElement GetRuleById(XElement ruleSet, string id)
        {
            Assert.ArgumentNotNull((object)ruleSet, nameof(ruleSet));
            Assert.ArgumentNotNull((object)id, nameof(id));
            string uid = ID.Parse(id).ToString();
            return ruleSet.Elements((XName)"rule").FirstOrDefault<XElement>((Func<XElement, bool>)(rule => rule.GetAttributeValue("uid") == uid));
        }

        /// <summary>The get rule condition html.</summary>
        /// <param name="rule">The rule.</param>
        /// <returns>The get rules html.</returns>
        private static string GetRuleConditionsHtml(XElement rule)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            HtmlTextWriter output = new HtmlTextWriter((TextWriter)new StringWriter());
            new RulesRenderer("<ruleset>" + (object)rule + "</ruleset>")
            {
                SkipActions = true
            }.Render(output);
            return output.InnerWriter.ToString();
        }

        /// <summary>
        /// Determines whether [is default condition] [the specified node].
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// <c>true</c> if [is default condition] [the specified node]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsDefaultCondition(XElement node)
        {
            Assert.ArgumentNotNull((object)node, nameof(node));
            return node.GetAttributeValue("uid") == ExtendedPersonalizationForm.DefaultConditionId;
        }

        /// <summary>Removes the action.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="id">The id.</param>
        private static void RemoveAction(XElement rule, string id)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)id, nameof(id));
            ExtendedPersonalizationForm.GetActionById(rule, id)?.Remove();
        }

        /// <summary>The add actions menu.</summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        private Menu GetActionsMenu(string id)
        {
            Assert.IsNotNullOrEmpty(id, nameof(id));
            Menu menu = new Menu();
            menu.ID = id + "_menu";
            string themedImageSource1 = Images.GetThemedImageSource("office/16x16/delete.png");
            string click1 = "javascript:Sitecore.CollapsiblePanel.remove(this, event, \"{0}\")".FormatWith((object)id);
            menu.Add("Delete", themedImageSource1, click1);
            string empty = string.Empty;
            string click2 = "javascript:Sitecore.CollapsiblePanel.renameAction(\"{0}\")".FormatWith((object)id);
            menu.Add("Rename", empty, click2);
            menu.AddDivider().ID = "moveDivider";
            string themedImageSource2 = Images.GetThemedImageSource("ApplicationsV2/16x16/navigate_up.png");
            string click3 = "javascript:Sitecore.CollapsiblePanel.moveUp(this, event, \"{0}\")".FormatWith((object)id);
            menu.Add("Move up", themedImageSource2, click3).ID = "moveUp";
            string themedImageSource3 = Images.GetThemedImageSource("ApplicationsV2/16x16/navigate_down.png");
            string click4 = "javascript:Sitecore.CollapsiblePanel.moveDown(this, event, \"{0}\")".FormatWith((object)id);
            menu.Add("Move down", themedImageSource3, click4).ID = "moveDown";
            return menu;
        }

        /// <summary>The get rule section html.</summary>
        /// <param name="rule">The rule.</param>
        /// <returns>The get rule section html.</returns>
        private string GetRuleSectionHtml(XElement rule)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            HtmlTextWriter writer = new HtmlTextWriter((TextWriter)new StringWriter());
            string str1 = ShortID.Parse(rule.GetAttributeValue("uid")).ToString();
            writer.Write("<table id='{ID}_body' cellspacing='0' cellpadding='0' class='rule-body'>");
            writer.Write("<tbody>");
            writer.Write("<tr>");
            writer.Write("<td class='left-column'>");
            this.RenderRuleConditions(rule, writer);
            writer.Write("</td>");
            writer.Write("<td class='right-column'>");
            this.RenderRuleActions(rule, writer);
            writer.Write("</td>");
            string str2 = this.RenderRulePlaceholder("afterAction", rule);
            writer.Write(str2);
            writer.Write("</tr>");
            writer.Write("</tbody>");
            writer.Write("</table>");
            string panelHtml = writer.InnerWriter.ToString().Replace("{ID}", str1);
            bool flag = ExtendedPersonalizationForm.IsDefaultCondition(rule);
            CollapsiblePanelRenderer.ActionsContext actionsContext = new CollapsiblePanelRenderer.ActionsContext()
            {
                IsVisible = !flag
            };
            if (!flag)
            {
                actionsContext.OnActionClick = "javascript:return Sitecore.CollapsiblePanel.showActionsMenu(this,event)";
                actionsContext.Menu = this.GetActionsMenu(str1);
            }
            string input = "Default";
            if (!flag || !string.IsNullOrEmpty(rule.GetAttributeValue("name")))
                input = rule.GetAttributeValue("name");
            CollapsiblePanelRenderer.NameContext nameContext = new CollapsiblePanelRenderer.NameContext(StringUtil.GetHtmlEncodedString(input))
            {
                Editable = !flag,
                OnNameChanged = "javascript:return Sitecore.CollapsiblePanel.renameComplete(this,event)"
            };
            return new CollapsiblePanelRenderer()
            {
                CssClass = "rule-container"
            }.Render(str1, panelHtml, nameContext, actionsContext);
        }

        /// <summary>Render the placeholder for the rule.</summary>
        /// <param name="placeholderName">The name of the placeholder to render.</param>
        /// <param name="rule">The rule to render.</param>
        /// <returns>The markup for the placeholder.</returns>
        private string RenderRulePlaceholder(string placeholderName, XElement rule)
        {
            if (this.ContextItem == null)
                return string.Empty;
            ItemUri uri = this.ContextItem.Uri;
            ID deviceId = ID.Parse(this.DeviceId);
            ID ruleSetId = ID.Parse(this.RenderingDefition.UniqueId);
            return RenderRulePlaceholderPipeline.Run(placeholderName, uri, deviceId, ruleSetId, rule);
        }

        /// <summary>
        /// Determines whether [is component displayed] in the specified rule.
        /// </summary>
        /// <param name="id">The rule id.</param>
        /// <returns>
        /// <c>true</c> if [is component displayed] [the specified id]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsComponentDisplayed(string id)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            XElement ruleById = ExtendedPersonalizationForm.GetRuleById(this.RulesSet, id);
            return ruleById == null || this.IsComponentDisplayed(ruleById);
        }

        /// <summary>
        /// Determines whether [is component displayed] [the specified rule].
        /// </summary>
        /// <param name="rule">The rule.</param>
        /// <returns>
        /// <c>true</c> if [is component displayed] [the specified rule]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsComponentDisplayed(XElement rule)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            return ExtendedPersonalizationForm.GetActionById(rule, this.HideRenderingActionId) == null;
        }

        /// <summary>Personalizes the component action exists.</summary>
        /// <returns>The component action exists.</returns>
        private bool PersonalizeComponentActionExists() => this.RulesSet.Elements((XName)"rule").Any<XElement>((Func<XElement, bool>)(rule => ExtendedPersonalizationForm.GetActionById(rule, this.SetRenderingActionId) != null));

        private bool HasRules()
        {
            bool flag = false;
            foreach (XElement element in this.RulesSet.Elements())
            {
                if (ExtendedPersonalizationForm.IsPersonalizedRule(element.GetAttributeValue("uid")))
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        private static bool IsPersonalizedRule(string ruleUniqueId) => !string.IsNullOrEmpty(ruleUniqueId) && ID.Parse(ruleUniqueId) != ItemIDs.Null;

        /// <summary>Set personalization tracking checkbox value.</summary>
        private void SetPersonalizationTrackingCheckbox()
        {
            if (this.HasRules())
                this.HasRulesPersonalizationTrackingCheckboxState();
            else
                this.NoRulesPersonalizationCheckboxState();
        }

        private void HasRulesPersonalizationTrackingCheckboxState()
        {
            if (this.HasPersonalizationTracking())
            {
                this.PersonalizationTracking.Checked = true;
                this.PersonalizationTracking.Disabled = false;
            }
            else
            {
                this.PersonalizationTracking.Checked = false;
                this.PersonalizationTracking.Disabled = false;
            }
        }

        private void NoRulesPersonalizationCheckboxState()
        {
            this.PersonalizationTracking.Checked = false;
            this.PersonalizationTracking.Disabled = true;
        }

        private bool HasPersonalizationTracking()
        {
            bool flag = false;
            if (!string.IsNullOrEmpty(this.RulesSet.GetAttributeValue("pet")))
                flag = bool.Parse(this.RulesSet.GetAttributeValue("pet"));
            return flag;
        }

        /// <summary>
        /// Check if personalization tracking checkbox is enabled.
        /// </summary>
        /// <returns>True if personalization tracking checkbox is enabled, otherwise false.</returns>
        private bool PersonalizationTrackingCheckboxEnabled() => this.PersonalizationTracking.Visible && !this.PersonalizationTracking.Disabled;

        /// <summary>Renders the hide rendering action.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="translatedText">The translated text.</param>
        /// <param name="isSelected">
        /// <c>true</c> if selected; otherwise, <c>false</c>.
        /// </param>
        /// <param name="index">The index.</param>
        /// <param name="style">The style.</param>
        private void RenderHideRenderingAction(
          HtmlTextWriter writer,
          string translatedText,
          bool isSelected,
          int index,
          string style)
        {
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            string str = "hiderenderingaction_{ID}_" + index.ToString((IFormatProvider)CultureInfo.InvariantCulture);
            writer.Write("<input id='" + str + "' type='radio' name='hiderenderingaction_{ID}' onfocus='this.blur();' onchange=\"javascript:if (this.checked) { scSwitchRendering(this, event, '{ID}'); }\" ");
            if (isSelected)
                writer.Write(" checked='checked' ");
            if (!string.IsNullOrEmpty(style))
                writer.Write(string.Format((IFormatProvider)CultureInfo.InvariantCulture, " style='{0}' ", (object)style));
            writer.Write("/>");
            writer.Write("<label for='" + str + "' class='section-header'>");
            writer.Write(translatedText);
            writer.Write("</label>");
        }

        /// <summary>Renders the picker.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="item">The item.</param>
        /// <param name="clickCommand">The click command.</param>
        /// <param name="resetCommand">The reset command.</param>
        /// <param name="prependEllipsis">
        /// if set to <c>true</c> [prepend ellipsis].
        /// </param>
        /// <param name="notSet">
        /// if set to <c>true</c> indicate the item was inferred, not set
        /// </param>
        private void RenderPicker(
          HtmlTextWriter writer,
          Item item,
          string clickCommand,
          string resetCommand,
          bool prependEllipsis,
          bool notSet = false)
        {
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            Assert.ArgumentNotNull((object)clickCommand, nameof(clickCommand));
            Assert.ArgumentNotNull((object)resetCommand, nameof(resetCommand));
            string themedImageSource = Images.GetThemedImageSource(item != null ? item.Appearance.Icon : string.Empty, ImageDimension.id16x16);
            string message1 = clickCommand + "(\\\"{ID}\\\")";
            string message2 = resetCommand + "(\\\"{ID}\\\")";
            string str1 = Translate.Text("[Not set]");
            string str2 = "item-picker";
            if (item != null)
                str1 = !notSet ? (prependEllipsis ? ".../" : string.Empty) + item.GetUIDisplayName() : str1 + (prependEllipsis ? ".../" : string.Empty) + " " + item.GetUIDisplayName();
            if (item == null | notSet)
                str2 += " not-set";
            writer.Write("<div style=\"background-image:url('{0}');background-position: left center;\" class='{1}'>", (object)HttpUtility.HtmlEncode(themedImageSource), (object)str2);
            writer.Write("<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", (object)Context.ClientPage.GetClientEvent(message1), (object)Translate.Text("Select"));
            writer.Write("<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", (object)Context.ClientPage.GetClientEvent(message2), (object)Translate.Text("Reset"));
            writer.Write("<span title=\"{0}\">{1}</span>", item == null ? (object)string.Empty : (object)item.GetUIDisplayName(), (object)str1);
            writer.Write("</div>");
        }

        private void RenderPicker(
          HtmlTextWriter writer,
          string datasource,
          string clickCommand,
          string resetCommand,
          bool prependEllipsis,
          bool notSet = false)
        {
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            Assert.ArgumentNotNull((object)clickCommand, nameof(clickCommand));
            Assert.ArgumentNotNull((object)resetCommand, nameof(resetCommand));
            string message1 = clickCommand + "(\\\"{ID}\\\")";
            string message2 = resetCommand + "(\\\"{ID}\\\")";
            string str1 = Translate.Text("[Not set]");
            string str2 = "item-picker";
            if (!datasource.IsNullOrEmpty())
                str1 = !notSet ? datasource : str1 + " " + datasource;
            if (datasource.IsNullOrEmpty() | notSet)
                str2 += " not-set";
            writer.Write(string.Format("<div class='{0}'>", (object)str2));
            writer.Write("<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", (object)Context.ClientPage.GetClientEvent(message1), (object)Translate.Text("Select"));
            writer.Write("<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", (object)Context.ClientPage.GetClientEvent(message2), (object)Translate.Text("Reset"));
            string str3 = str1;
            if (str3 != null && str3.Length > 15)
                str3 = str3.Substring(0, 14) + "...";
            writer.Write("<span title=\"{0}\">{1}</span>", (object)str1, (object)str3);
            writer.Write("</div>");
        }

        /// <summary>The render rule actions.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="writer">The writer.</param>
        private void RenderRuleActions(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            bool isSelected = this.IsComponentDisplayed(rule);
            writer.Write("<div id='{ID}_hiderendering' class='hide-rendering'>");
            this.RenderHideRenderingAction(writer, Translate.Text("Show"), isSelected, 0, (string)null);
            this.RenderHideRenderingAction(writer, Translate.Text("Hide"), !isSelected, 1, "margin-left:35px;");
            writer.Write("</div>");
            string str1 = isSelected ? string.Empty : " display-off";
            string str2 = this.ComponentPersonalization.Checked ? string.Empty : " style='display:none'";
            writer.Write("<div id='{ID}_setrendering' class='set-rendering" + str1 + "'" + str2 + ">");
            this.RenderSetRenderingAction(rule, writer);
            writer.Write("</div>");
            writer.Write("<div id='{ID}_setdatasource' class='set-datasource" + str1 + "'>");
            this.RenderSetDatasourceAction(rule, writer);
            writer.Write("</div>");
        }

        /// <summary>The render rule conditions.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="writer">The writer.</param>
        private void RenderRuleConditions(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            bool flag = ExtendedPersonalizationForm.IsDefaultCondition(rule);
            if (!flag)
            {
                Button button1 = new Button();
                button1.Header = Translate.Text("Edit rule");
                button1.ToolTip = Translate.Text("Edit this rule");
                button1.Class = "scButton edit-button";
                button1.Click = "EditConditionClick(\\\"{ID}\\\")";
                Button button2 = button1;
                writer.Write(HtmlUtil.RenderControl((System.Web.UI.Control)button2));
            }
            string str = !flag ? "condition-container" : "condition-container default";
            writer.Write("<div id='{ID}_rule' class='" + str + "'>");
            writer.Write(flag ? this.ConditionDescriptionDefault : ExtendedPersonalizationForm.GetRuleConditionsHtml(rule));
            writer.Write("</div>");
        }

        /// <summary>The render rules.</summary>
        private void RenderRules()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<div id='non-default-container'>");
            foreach (XElement element in this.RulesSet.Elements((XName)"rule"))
            {
                if (ExtendedPersonalizationForm.IsDefaultCondition(element))
                    stringBuilder.Append("</div>");
                stringBuilder.Append(this.GetRuleSectionHtml(element));
            }
            this.RulesContainer.InnerHtml = stringBuilder.ToString();
        }

        /// <summary>Renders the set datasource action.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="writer">The writer.</param>
        private void RenderSetDatasourceAction(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            string datasource = this.RenderingDefition.Datasource;
            XElement actionById = ExtendedPersonalizationForm.GetActionById(rule, this.SetDatasourceActionId);
            bool flag = true;
            string str;
            if (actionById != null)
            {
                str = actionById.GetAttributeValue("DataSource");
                flag = false;
            }
            else
                str = string.Empty;
            bool notSet = false;
            Item contextItem;
            if (!string.IsNullOrEmpty(str))
            {
                contextItem = Sitecore.Client.ContentDatabase.GetItem(str);
            }
            else
            {
                contextItem = this.ContextItem;
                notSet = true;
            }
            writer.Write("<div " + (!flag ? string.Empty : "class='default-values'") + ">");
            writer.Write("<span class='section-header' unselectable='on'>");
            writer.Write(Translate.Text("Content:"));
            writer.Write("</span>");
            if (contextItem == null)
                this.RenderPicker(writer, str, "SetDatasourceClick", "ResetDatasource", !notSet, notSet);
            else
            {
                this.RenderPicker(writer, contextItem, "SetDatasourceClick", "ResetDatasource", !notSet, notSet);
                writer.Write("<div class='section-header custom-datasource-path' style='word-wrap:break-word;color:#990099; margin:8px 0px; width: 380px;'>");
                writer.Write(contextItem?.Paths?.FullPath);
                writer.Write("</div>");
            }
                
            writer.Write("</div>");
        }

        /// <summary>Renders the set rendering action.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="writer">The writer.</param>
        private void RenderSetRenderingAction(XElement rule, HtmlTextWriter writer)
        {
            Assert.ArgumentNotNull((object)rule, nameof(rule));
            Assert.ArgumentNotNull((object)writer, nameof(writer));
            string path = this.RenderingDefition.ItemID;
            XElement actionById = ExtendedPersonalizationForm.GetActionById(rule, this.SetRenderingActionId);
            bool flag = true;
            if (actionById != null)
            {
                string attributeValue = actionById.GetAttributeValue("RenderingItem");
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    path = attributeValue;
                    flag = false;
                }
            }
            writer.Write("<div " + (!flag ? string.Empty : "class='default-values'") + ">");
            if (string.IsNullOrEmpty(path))
            {
                writer.Write("</div>");
            }
            else
            {
                Item obj = Sitecore.Client.ContentDatabase.GetItem(path);
                if (obj == null)
                {
                    writer.Write("</div>");
                }
                else
                {
                    writer.Write("<span class='section-header' unselectable='on'>");
                    writer.Write(Translate.Text("Presentation:"));
                    writer.Write("</span>");
                    string s = Images.GetThemedImageSource(obj.Appearance.Icon, ImageDimension.id48x48);
                    if (!string.IsNullOrEmpty(obj.Appearance.Thumbnail) && obj.Appearance.Thumbnail != Settings.DefaultThumbnail)
                    {
                        string thumbnailSrc = UIUtil.GetThumbnailSrc(obj, 128, 128);
                        if (!string.IsNullOrEmpty(thumbnailSrc))
                            s = thumbnailSrc;
                    }
                    writer.Write("<div style=\"background-image:url('{0}')\" class='thumbnail-container'>", (object)HttpUtility.HtmlEncode(s));
                    writer.Write("</div>");
                    writer.Write("<div class='picker-container'>");
                    this.RenderPicker(writer, obj, "SetRenderingClick", "ResetRendering", false);
                    writer.Write("</div>");
                    writer.Write("</div>");
                }
            }
        }
    }
}
