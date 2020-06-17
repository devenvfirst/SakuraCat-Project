// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Text;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a [choice](/guide/choices.md) option to a choice handler with the specified ID (or default one).
    /// </summary>
    /// <remarks>
    /// When `goto`, `gosub` and `do` parameters are not specified, will continue script execution from the next script line.
    /// </remarks>
    /// <example>
    /// ; Print the text, then immediately show choices and stop script execution.
    /// Continue executing this script or ...?[skipInput]
    /// @choice "Continue"
    /// @choice "Load another script from start" goto:AnotherScript
    /// @choice "Load another script from \"MyLabel\" label" goto:AnotherScript.MyLabel
    /// @choice "Goto to \"MySub\" subroutine in another script" gosub:AnotherScript.MySub
    /// @stop
    /// 
    /// ; You can also set custom variables based on choices.
    /// @choice "I'm humble, one is enough..." set:score++
    /// @choice "Two, please." set:score=score+2
    /// @choice "I'll take the entire stock!" set:karma--;score=999
    /// 
    /// ; Play a sound effect and arrange characters when choice is picked
    /// @choice "Arrange" goto:.Continue do:"@sfx Click, @arrange k.10\,y.55"
    /// 
    /// ; Following example shows how to make an interactive map via `@choice` commands.
    /// ; For this example, we assume, that inside a `Resources/MapButtons` folder you've 
    /// ; stored prefabs with `ChoiceHandlerButton` component attached to their root objects.
    /// # Map
    /// @back Map
    /// @hidePrinter
    /// @choice handler:ButtonArea button:MapButtons/Home pos:-300,-300 goto:.HomeScene
    /// @choice handler:ButtonArea button:MapButtons/Shop pos:300,200 goto:.ShopScene
    /// @stop
    /// 
    /// # HomeScene
    /// @back Home
    /// Home, sweet home!
    /// @goto .Map
    /// 
    /// # ShopScene
    /// @back Shop
    /// Don't forget about cucumbers!
    /// @goto .Map
    /// </example>
    [CommandAlias("choice")]
    public class AddChoice : Command, Command.ILocalizable, Command.IPreloadable
    {
        /// <summary>
        /// Text to show for the choice.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter ChoiceSummary;
        /// <summary>
        /// Path (relative to a `Resources` folder) to a [button prefab](/guide/choices.md#choice-button) representing the choice. 
        /// The prefab should have a `ChoiceHandlerButton` component attached to the root object.
        /// Will use a default button when not provided.
        /// </summary>
        [ParameterAlias("button")]
        public StringParameter ButtonPath;
        /// <summary>
        /// Local position of the choice button inside the choice handler (if supported by the handler implementation).
        /// </summary>
        [ParameterAlias("pos")]
        public DecimalListParameter ButtonPosition;
        /// <summary>
        /// ID of the choice handler to add choice for. Will use a default handler if not provided.
        /// </summary>
        [ParameterAlias("handler")]
        public StringParameter HandlerId;
        /// <summary>
        /// Path to go when the choice is selected by user;
        /// see [@goto] command for the path format.
        /// </summary>
        [ParameterAlias("goto")]
        public NamedStringParameter GotoPath;
        /// <summary>
        /// Path to a subroutine to go when the choice is selected by user;
        /// see [@gosub] command for the path format. When `goto` is assigned this parameter will be ignored.
        /// </summary>
        [ParameterAlias("gosub")]
        public NamedStringParameter GosubPath;
        /// <summary>
        /// Set expression to execute when the choice is selected by user; 
        /// see [@set] command for syntax reference.
        /// </summary>
        [ParameterAlias("set")]
        public StringParameter SetExpression;
        /// <summary>
        /// Script commands to execute when the choice is selected by user; 
        /// don't forget to escape commas inside list values to prevent them being treated as delimiters.
        /// The commands will be invoked in order after `set`, `goto` and `gosub` are handled (if assigned).
        /// </summary>
        [ParameterAlias("do")]
        public StringListParameter OnSelected;
        /// <summary>
        /// Whether to also show choice handler the choice is added for;
        /// enabled by default.
        /// </summary>
        [ParameterAlias("show")]
        public BooleanParameter ShowHandler = true;
        /// <summary>
        /// Duration (in seconds) of the fade-in (reveal) animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        protected IChoiceHandlerManager HandlerManager => Engine.GetService<IChoiceHandlerManager>();

        public async UniTask HoldResourcesAsync ()
        {
            if (!Assigned(HandlerId) || HandlerId.DynamicValue) return;

            var handlerId = Assigned(HandlerId) ? HandlerId.Value : HandlerManager.Configuration.DefaultHandlerId;
            var handler = await HandlerManager.GetOrAddActorAsync(handlerId);
            await handler.HoldResourcesAsync(this, null);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(HandlerId) || HandlerId.DynamicValue) return;

            var handlerId = Assigned(HandlerId) ? HandlerId.Value : HandlerManager.Configuration.DefaultHandlerId;
            if (HandlerManager.ActorExists(handlerId)) HandlerManager.GetActor(handlerId).ReleaseResources(this, null);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var handlerId = Assigned(HandlerId) ? HandlerId.Value : HandlerManager.Configuration.DefaultHandlerId;
            var choiceHandler = await HandlerManager.GetOrAddActorAsync(handlerId);
            if (cancellationToken.IsCancellationRequested) return;

            if (!choiceHandler.Visible && ShowHandler)
                choiceHandler.ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken).Forget();

            var builder = new StringBuilder();

            if (Assigned(SetExpression))
                builder.AppendLine($"{CommandScriptLine.IdentifierLiteral}{nameof(SetCustomVariable)} {SetExpression}");

            if (Assigned(GotoPath))
                builder.AppendLine($"{CommandScriptLine.IdentifierLiteral}{nameof(Goto)} {GotoPath.Name ?? string.Empty}{(GotoPath.NamedValue.HasValue ? $".{GotoPath.NamedValue.Value}" : string.Empty)}");
            else if (Assigned(GosubPath))
                builder.AppendLine($"{CommandScriptLine.IdentifierLiteral}{nameof(Gosub)} {GosubPath.Name ?? string.Empty}{(GosubPath.NamedValue.HasValue ? $".{GosubPath.NamedValue.Value}" : string.Empty)}");

            if (Assigned(OnSelected))
                foreach (var line in OnSelected)
                    builder.AppendLine(line);

            var onSelectScript = builder.ToString()?.TrimFull();
            var buttonPos = Assigned(ButtonPosition) ? (Vector2?)ArrayUtils.ToVector2(ButtonPosition) : null;
            var autoPlay = !Assigned(GotoPath) && !Assigned(GosubPath) && !Assigned(OnSelected);

            var choice = new ChoiceState(ChoiceSummary, ButtonPath, buttonPos, onSelectScript, autoPlay);
            choiceHandler.AddChoice(choice);
        }
    }
}
