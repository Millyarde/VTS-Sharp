using System;

using static VTS.VTSArtMeshListRequestData;
using static VTS.VTSArtMeshSelectionRequestData;
using static VTS.VTSArtMeshSelectionResponseData;
using static VTS.VTSAuthTokenRequestData;
using static VTS.VTSAvailableModelsRequestData;
using static VTS.VTSColorTintRequestData;
using static VTS.VTSCurrentModelRequestData;
using static VTS.VTSErrorData;
using static VTS.VTSEventSubscriptionRequestData;
using static VTS.VTSExpressionActivationRequestData;
using static VTS.VTSExpressionStateRequestData;
using static VTS.VTSFaceFoundRequestData;
using static VTS.VTSFolderInfoRequestData;
using static VTS.VTSGetCurrentModelPhysicsRequestData;
using static VTS.VTSHotkeysInCurrentModelRequestData;
using static VTS.VTSHotkeyTriggerRequestData;
using static VTS.VTSInjectParameterDataRequestData;
using static VTS.VTSInputParameterListRequestData;
using static VTS.VTSItemAnimationControlRequestData;
using static VTS.VTSItemAnimationControlResponseData;
using static VTS.VTSItemListRequestData;
using static VTS.VTSItemListResponseData;
using static VTS.VTSItemLoadRequestData;
using static VTS.VTSItemLoadResponseData;
using static VTS.VTSItemMoveRequestData;
using static VTS.VTSItemMoveResponseData;
using static VTS.VTSItemUnloadRequestData;
using static VTS.VTSItemUnloadResponseData;
using static VTS.VTSLive2DParameterListRequestData;
using static VTS.VTSModelLoadRequestData;
using static VTS.VTSMoveModelRequestData;
using static VTS.VTSNDIConfigRequestData;
using static VTS.VTSParameterCreationRequestData;
using static VTS.VTSParameterDeletionRequestData;
using static VTS.VTSParameterValueRequestData;
using static VTS.VTSSceneColorOverlayInfoRequestData;
using static VTS.VTSSetCurrentModelPhysicsRequestData;
using static VTS.VTSStateRequestData;
using static VTS.VTSStatisticsRequestData;
using static VTS.VTSVTubeStudioAPIStateBroadcastData;

namespace VTS
{
    #region Common
    public class VTSMessageData<T> where T : DTO
    {
        public string APIName = "VTubeStudioPublicAPI";
        public long Timestamp;
        public string APIVersion = "1.0";
        public string RequestID = Guid.NewGuid().ToString();
        public string MessageType;
        public T Data;

        public VTSMessageData(MessageTypeEnum e)
        {
            MessageType = e.GetDescription();
        }

        public VTSMessageData(MessageTypeEnum e, T d)
        {
            MessageType = e.GetDescription();
            Data = d;
        }
    }

    public class VTSErrorData : VTSMessageData<ErrorData>
    {
        public VTSErrorData() : base(MessageTypeEnum.APIError) { }
        public VTSErrorData(string e) : base(MessageTypeEnum.APIError, new ErrorData(e)) { }

        public class ErrorData : DTO
        {
            public ErrorData() { }
            public ErrorData(string message) { Message = message; }

            public ErrorID ErrorID;
            public string Message;
        }
    }
    #endregion

    #region General API

    public class VTSStateRequestData : VTSMessageData<StateRequestData>
    {
        public VTSStateRequestData() : base(MessageTypeEnum.APIStateRequest) { }

        public class StateRequestData : DTO
        {
            public bool Active;
            public string VTubeStudioVersion;
            public bool CurrentSessionAuthenticated;
        }
    }

    public class VTSAuthTokenRequestData : VTSMessageData<AuthRequestData>
    {
        public VTSAuthTokenRequestData() : base(MessageTypeEnum.AuthenticationTokenRequest) { }

        public class AuthRequestData : DTO
        {
            public string PluginName;
            public string PluginDeveloper;
            public string PluginIcon;
            public string AuthenticationToken;
            public bool Authenticated;
            public string Reason;
        }
    }

    public class VTSStatisticsRequestData : VTSMessageData<StatisticsRequestData>
    {
        public VTSStatisticsRequestData() : base(MessageTypeEnum.StatisticsRequest) { }

        public class StatisticsRequestData : DTO
        {
            public long Uptime;
            public int Framerate;
            public int AllowedPlugins;
            public int ConnectedPlugins;
            public bool StartedWithSteam;
            public int WindowWidth;
            public int WindowHeight;
            public bool WindowIsFullscreen;
        }
    }

    public class VTSFolderInfoRequestData : VTSMessageData<FolderInfoRequestData>
    {
        public VTSFolderInfoRequestData() : base(MessageTypeEnum.FolderInfoRequest) { }

        public class FolderInfoRequestData : DTO
        {
            public string Models;
            public string Backgrounds;
            public string Items;
            public string Config;
            public string Logs;
            public string Backup;
        }
    }
    
    public class VTSCurrentModelRequestData : VTSMessageData<CurrentModelRequestData>
    {
        public VTSCurrentModelRequestData() : base(MessageTypeEnum.CurrentModelRequest) { }

        public class CurrentModelRequestData : VTSModelData
        {
            public string Live2DModelName;
            public long ModelLoadTime;
            public long TimeSinceModelLoaded;
            public int NumberOfLive2DParameters;
            public int NumberOfLive2DArtmeshes;
            public bool HasPhysicsFile;
            public int NumberOfTextures;
            public int TextureResolution;
            public ModelPosition ModelPosition;
        }
    }

    public class VTSAvailableModelsRequestData : VTSMessageData<AvailableModelsRequestData>
    {
        public VTSAvailableModelsRequestData() : base(MessageTypeEnum.AvailableModelsRequest) { }

        public class AvailableModelsRequestData : DTO
        {
            public int NumberOfModels;
            public VTSModelData[] AvailableModels;
        }
    }

    public class VTSModelLoadRequestData : VTSMessageData<ModelLoadRequestData>
    {
        public VTSModelLoadRequestData() : base(MessageTypeEnum.ModelLoadRequest) { }

        public class ModelLoadRequestData : DTO
        {
            public string ModelID;
        }
    }

    public class VTSMoveModelRequestData : VTSMessageData<MoveModelRequestData>
    {
        public VTSMoveModelRequestData() : base(MessageTypeEnum.MoveModelRequest) { }

        public class MoveModelRequestData : ModelPosition
        {
            public float TimeInSeconds;
            public bool ValuesAreRelativeToModel;
        }
    }

    public class VTSHotkeysInCurrentModelRequestData : VTSMessageData<HotkeysInCurrentModelRequestData>
    {
        public VTSHotkeysInCurrentModelRequestData() : base(MessageTypeEnum.HotkeysInCurrentModelRequest) { }

        public class HotkeysInCurrentModelRequestData : DTO
        {
            public bool ModelLoaded;
            public string ModelName;
            public string ModelID;
            public string Live2DItemFileName;
            public HotkeyData[] AvailableHotkeys;
        }
    }

    public class VTSHotkeyTriggerRequestData : VTSMessageData<HotkeyTriggerRequestData>
    {
        public VTSHotkeyTriggerRequestData() : base(MessageTypeEnum.HotkeyTriggerRequest) { }

        public class HotkeyTriggerRequestData : DTO
        {
            public string HotkeyID;
            public string ItemInstanceID;
        }
    }

    public class VTSArtMeshListRequestData : VTSMessageData<ArtMeshListRequestData>
    {
        public VTSArtMeshListRequestData() : base(MessageTypeEnum.ArtMeshListRequest) { }

        public class ArtMeshListRequestData : DTO
        {
            public bool ModelLoaded;
            public int NumberOfArtMeshNames;
            public int NumberOfArtMeshTags;
            public string[] ArtMeshNames;
            public string[] ArtMeshTags;
        }
    }

    public class VTSColorTintRequestData : VTSMessageData<ColorTintRequestData>
    {
        public VTSColorTintRequestData() : base(MessageTypeEnum.ColorTintRequest) { }

        public class ColorTintRequestData : DTO
        {
            public ArtMeshColorTint ColorTint;
            public ArtMeshMatcher ArtMeshMatcher;
            public int MatchedArtMeshes;
        }
    }

    public class VTSSceneColorOverlayInfoRequestData : VTSMessageData<SceneColorOverlayInfoRequestData>
    {
        public VTSSceneColorOverlayInfoRequestData() : base(MessageTypeEnum.SceneColorOverlayInfoRequest) { }

        public class SceneColorOverlayInfoRequestData : DTO
        {
            public bool Active;
            public bool ItemsIncluded;
            public bool IsWindowCapture;
            public int BaseBrightness;
            public int ColorBoost;
            public int Smoothing;
            public int ColorOverlayR;
            public int ColorOverlayG;
            public int ColorOverlayB;
            public ColorCapturePart LeftCapturePart;
            public ColorCapturePart MiddleCapturePart;
            public ColorCapturePart RightCapturePart;
        }
    }

    public class VTSFaceFoundRequestData : VTSMessageData<FaceFoundRequestData>
    {
        public VTSFaceFoundRequestData() : base(MessageTypeEnum.FaceFoundRequest) { }

        public class FaceFoundRequestData : DTO
        {
            public bool Found;
        }
    }

    public class VTSInputParameterListRequestData : VTSMessageData<InputParameterListRequestData>
    {
        public VTSInputParameterListRequestData() : base(MessageTypeEnum.InputParameterListRequest) { }

        public class InputParameterListRequestData : DTO
        {
            public bool ModelLoaded;
            public string ModelName;
            public string ModelID;
            public VTSParameter[] CustomParameters;
            public VTSParameter[] DefaultParameters;
        }
    }

    public class VTSParameterValueRequestData : VTSMessageData<ParameterValueRequestData>
    {
        public VTSParameterValueRequestData() : base(MessageTypeEnum.ParameterValueRequest) { }

        public class ParameterValueRequestData : VTSParameter { }
    }

    public class VTSLive2DParameterListRequestData : VTSMessageData<Live2DParameterListRequestData>
    {
        public VTSLive2DParameterListRequestData() : base(MessageTypeEnum.Live2DParameterListRequest) { }

        public class Live2DParameterListRequestData : DTO
        {
            public bool ModelLoaded;
            public string ModelName;
            public string ModelID;
            public VTSParameter[] Parameters;
        }
    }

    public class VTSParameterCreationRequestData : VTSMessageData<ParameterCreationRequestData>
    {
        public VTSParameterCreationRequestData() : base(MessageTypeEnum.ParameterCreationRequest) { }

        public class ParameterCreationRequestData : VTSCustomParameter { }
    }

    public class VTSParameterDeletionRequestData : VTSMessageData<ParameterDeletionRequestData>
    {
        public VTSParameterDeletionRequestData() : base(MessageTypeEnum.ParameterDeletionRequest) { }

        public class ParameterDeletionRequestData : DTO
        {
            public string ParameterName;
        }
    }

    public class VTSInjectParameterDataRequestData : VTSMessageData<InjectParameterDataRequestData>
    {
        public VTSInjectParameterDataRequestData() : base(MessageTypeEnum.InjectParameterDataRequest) { }

        public class InjectParameterDataRequestData : DTO
        {
            public string Mode;
            public bool FaceFound;
            public VTSParameterInjectionValue[] ParameterValues;
        }
    }

    public class VTSExpressionStateRequestData : VTSMessageData<ExpressionStateRequestData>
    {
        public VTSExpressionStateRequestData() : base(MessageTypeEnum.ExpressionStateRequest) { }

        public class ExpressionStateRequestData : DTO
        {
            public bool Details = true;
            public string ExpressionFile;
            public bool ModelLoaded;
            public string ModelName;
            public string ModelID;
            public ExpressionData[] Expressions;
        }
    }

    public class VTSExpressionActivationRequestData : VTSMessageData<ExpressionActivationRequestData>
    {
        public VTSExpressionActivationRequestData() : base(MessageTypeEnum.ExpressionActivationRequest) { }

        public class ExpressionActivationRequestData : DTO
        {
            public string ExpressionFile;
            public bool Active;
        }
    }

    public class VTSGetCurrentModelPhysicsRequestData : VTSMessageData<GetCurrentModelPhysicsRequestData>
    {
        public VTSGetCurrentModelPhysicsRequestData() : base(MessageTypeEnum.GetCurrentModelPhysicsRequest) { }

        public class GetCurrentModelPhysicsRequestData : DTO
        {
            public bool ModelLoaded;
            public string ModelName;
            public string ModelID;
            public bool ModelHasPhysics;
            public bool PhysicsSwitchedOn;
            public bool UsingLegacyPhysics;
            public int PhysicsFPSSetting;
            public int BaseStrength;
            public int BaseWind;
            public bool APIPhysicsOverrideActive;
            public string APIPhysicsOverridePluginName;
            public VTSPhysicsGroup[] PhysicsGroups;
        }
    }

    public class VTSSetCurrentModelPhysicsRequestData : VTSMessageData<SetCurrentModelPhysicsRequestData>
    {
        public VTSSetCurrentModelPhysicsRequestData() : base(MessageTypeEnum.SetCurrentModelPhysicsRequest) { }

        public class SetCurrentModelPhysicsRequestData : DTO
        {
            public VTSPhysicsOverride[] StrengthOverrides;
            public VTSPhysicsOverride[] WindOverrides;
        }
    }

    public class VTSNDIConfigRequestData : VTSMessageData<NDIConfigRequestData>
    {
        public VTSNDIConfigRequestData() : base(MessageTypeEnum.NDIConfigRequest) { }

        public class NDIConfigRequestData : DTO
        {
            public bool SetNewConfig;
            public bool NDIActive;
            public bool UseNDI5;
            public bool UseCustomResolution;
            public int CustomWidthNDI;
            public int CustomHeightNDI;
        }
    }
    public class VTSVTubeStudioAPIStateBroadcastData : VTSMessageData<VTubeStudioAPIStateBroadcastData>
    {
        public VTSVTubeStudioAPIStateBroadcastData() : base(MessageTypeEnum.VTubeStudioAPIStateBroadcast) { }

        public class VTubeStudioAPIStateBroadcastData : DTO
        {
            public bool Active;
            public int Port;
            public string InstanceID;
            public string WindowTitle;
        }
    }

    public class VTSItemListRequestData : VTSMessageData<ItemListRequestData>
    {
        public VTSItemListRequestData() : base(MessageTypeEnum.ItemListRequest) { }

        public class ItemListRequestData : DTO
        {
            public bool IncludeAvailableSpots;
            public bool IncludeItemInstancesInScene;
            public bool IncludeAvailableItemFiles;
            public string OnlyItemsWithFileName;
            public string OnlyItemsWithInstanceID;
        }
    }

    public class VTSItemListResponseData : VTSMessageData<ItemListResponseData>
    {
        public VTSItemListResponseData() : base(MessageTypeEnum.ItemListResponse) { }

        public class ItemListResponseData : DTO
        {
            public int ItemsInSceneCount;
            public int TotalItemsAllowedCount;
            public bool CanLoadItemsRightNow;
            public int[] AvailableSpots;
            public ItemInstance[] ItemInstancesInScene;
            public ItemFile[] AvailableItemFiles;
        }
    }

    public class VTSItemLoadRequestData : VTSMessageData<ItemLoadRequestData>
    {
        public VTSItemLoadRequestData() : base(MessageTypeEnum.ItemLoadRequest) { }

        public class ItemLoadRequestData : DTO
        {
            public string FileName;
            public float PositionX;
            public float PositionY;
            public float Size;
            public float Rotation;
            public float FadeTime;
            public int Order;
            public bool FailIfOrderTaken;
            public float Smoothing;
            public bool Censored;
            public bool Flipped;
            public bool Locked;
            public bool UnloadWhenPluginDisconnects;
        }
    }

    public class VTSItemLoadResponseData : VTSMessageData<ItemLoadResponseData>
    {
        public VTSItemLoadResponseData() : base(MessageTypeEnum.ItemLoadResponse) { }

        public class ItemLoadResponseData : DTO
        {
            public string InstanceID;
        }
    }

    public class VTSItemUnloadRequestData : VTSMessageData<ItemUnloadRequestData>
    {
        public VTSItemUnloadRequestData() : base(MessageTypeEnum.ItemUnloadRequest) { }

        public class ItemUnloadRequestData : DTO
        {
            public bool UnloadAllInScene;
            public bool UnloadAllLoadedByThisPlugin;
            public bool AllowUnloadingItemsLoadedByUserOrOtherPlugins;
            public string[] InstanceIDs;
            public string[] FileNames;
        }
    }

    public class VTSItemUnloadResponseData : VTSMessageData<ItemUnloadResponseData>
    {
        public VTSItemUnloadResponseData() : base(MessageTypeEnum.ItemUnloadResponse) { }

        public class ItemUnloadResponseData : DTO
        {
            public UnloadedItem[] UnloadedItems;
        }
    }

    public class VTSItemAnimationControlRequestData : VTSMessageData<ItemAnimationControlRequestData>
    {
        public VTSItemAnimationControlRequestData() : base(MessageTypeEnum.ItemAnimationControlRequest) { }

        public class ItemAnimationControlRequestData : DTO
        {
            public string ItemInstanceID;
            public int Framerate;
            public int Frame;
            public float Brightness;
            public float Opacity;
            public bool SetAutoStopFrames;
            public int[] AutoStopFrames;
            public bool SetAnimationPlayState;
            public bool AnimationPlayState;
        }
    }

    public class VTSItemAnimationControlResponseData : VTSMessageData<ItemAnimationControlResponseData>
    {
        public VTSItemAnimationControlResponseData() : base(MessageTypeEnum.ItemAnimationControlResponse) { }

        public class ItemAnimationControlResponseData : DTO
        {
            public int Frame;
            public bool AnimationPlaying;
        }
    }

    public class VTSItemMoveRequestData : VTSMessageData<ItemMoveRequestData>
    {
        public VTSItemMoveRequestData() : base(MessageTypeEnum.ItemMoveRequest) { }

        public class ItemMoveRequestData : DTO
        {
            public VTSItemToMove[] ItemsToMove;
        }
    }

    public class VTSItemMoveResponseData : VTSMessageData<ItemMoveResponseData>
    {
        public VTSItemMoveResponseData() : base(MessageTypeEnum.ItemMoveResponse) { }

        public class ItemMoveResponseData : DTO
        {
            public MovedItem[] MovedItems;
        }
    }

    public class VTSArtMeshSelectionRequestData : VTSMessageData<ArtMeshSelectionRequestData>
    {
        public VTSArtMeshSelectionRequestData() : base(MessageTypeEnum.ArtMeshSelectionRequest) { }

        public class ArtMeshSelectionRequestData : DTO
        {
            public string TextOverride;
            public string HelpOverride;
            public int RequestedArtMeshCount;
            public string[] ActiveArtMeshes;

        }
    }

    public class VTSArtMeshSelectionResponseData : VTSMessageData<ArtMeshSelectionResponseData>
    {
        public VTSArtMeshSelectionResponseData() : base(MessageTypeEnum.ArtMeshSelectionResponse) { }

        public class ArtMeshSelectionResponseData : DTO
        {
            public bool Success;
            public string[] ActiveArtMeshes;
            public string[] InactiveArtMeshes;
        }
    }

    #endregion

    #region Event API

    public class VTSEventSubscriptionRequestData : VTSMessageData<VTSEventSubscriptonRequestData>
    {
        public VTSEventSubscriptionRequestData(MessageTypeEnum e) : base(e) { }

        public class VTSEventSubscriptonRequestData : DTO
        {
            public string EventName;
            public bool Subscribe;
            public VTSEventConfigData Config;
        }
    }

    public class VTSEventConfigData : DTO { }

    public class VTSEventData : VTSMessageData<DTO> { public VTSEventData(MessageTypeEnum e) : base(e) { } }

    public class VTSEventSubscriptionResponseData : VTSMessageData<DTO>
    {
        public VTSEventSubscriptionResponseData() : base(MessageTypeEnum.EventSubscriptionResponse) { }

        public class EventSubscriptionResponseData : DTO
        {
            public int SubscribedEventCount;
            public string[] SubscribedEvents;
        }
    }

    // Test Event

    public class VTSTestEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSTestEventSubscriptionRequestData() : base(MessageTypeEnum.TestEventSubscription) { }

        public class TestEventSubscriptionRequestData : VTSEventSubscriptonRequestData
        {
            public void SetEventName(string eventName)
            {
                EventName = eventName;
            }

            public string GetEventName()
            {
                return EventName;
            }

            public void SetSubscribed(bool subscribe)
            {
                Subscribe = subscribe;
            }

            public bool GetSubscribed()
            {
                return Subscribe;
            }

            public void SetConfig(VTSEventConfigData config)
            {
                Config = config;
            }
        }
    }

    /// <summary>
    /// A container for providing subscription options for a Test Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event</a>
    /// </summary>
    public class VTSTestEventConfigOptions : VTSEventConfigData
    {
        public VTSTestEventConfigOptions()
        {
            TestMessageForEvent = null; // TODO CHECK THIS
        }

        public VTSTestEventConfigOptions(string message)
        {
            TestMessageForEvent = message;
        }

        public string TestMessageForEvent;
    }

    public class VTSTestEventData : VTSEventData
    {
        public VTSTestEventData() : base(MessageTypeEnum.TestEvent) { }

        public class TestEventData
        {
            public string YourTestMessage;
            public long Counter;
        }
    }

    // Model Loaded Event

    public class VTSModelLoadedEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSModelLoadedEventSubscriptionRequestData() : base(MessageTypeEnum.EventSubscriptionRequest) { }
    }

    /// <summary>
    /// A container for providing subscription options for a Model Loaded Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded</a>
    /// </summary>
    public class VTSModelLoadedEventConfigOptions : VTSEventConfigData
    {
        public VTSModelLoadedEventConfigOptions()
        {
            ModelID = null; // TODO check this
        }

        public string ModelID;
    }

    public class VTSModelLoadedEventData: VTSEventData
    {
        public VTSModelLoadedEventData() : base(MessageTypeEnum.ModelLoadedEvent) { } 


        public class ModelLoadedEventData : ModelData { }
    }

    // Tracking Changed Event

    public class VTSTrackingEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSTrackingEventSubscriptionRequestData() : base(MessageTypeEnum.EventSubscriptionRequest) { }

        public class TrackingEventSubscriptionRequestData : VTSEventSubscriptonRequestData { }
    }

    /// <summary>
    /// A container for providing subscription options for a Lost Tracking Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking</a>
    /// </summary>
    public class VTSTrackingEventConfigOptions : VTSEventConfigData
    {
        public VTSTrackingEventConfigOptions() { }
    }

    public class VTSTrackingEventData : VTSEventData
    {
        public VTSTrackingEventData() : base(MessageTypeEnum.TrackingStatusChangedEvent) { }

        public class TrackingEventData
        {
            public bool FaceFound;
            public bool LeftHandFound;
            public bool RightHandFound;
        }
    }

    // Background Changed Event

    public class VTSBackgroundChangedEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSBackgroundChangedEventSubscriptionRequestData() : base(MessageTypeEnum.EventSubscriptionRequest) { }

        public class BackgroundChangedEventSubscriptionRequestData : VTSEventSubscriptonRequestData { }
    }

    /// <summary>
    /// A container for providing subscription options for a Background Changed Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#background-changed">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#background-changed</a>
    /// </summary>
    public class VTSBackgroundChangedEventConfigOptions : VTSEventConfigData
    {
        public VTSBackgroundChangedEventConfigOptions() { }
    }

    public class VTSBackgroundChangedEventData : VTSEventData
    {
        public VTSBackgroundChangedEventData() : base(MessageTypeEnum.BackgroundChangedEvent) { }

        public class BackgroundChangedEventData
        {
            public string BackgroundName;
        }
    }

    // Model Config Changed Event

    public class VTSModelConfigChangedEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSModelConfigChangedEventSubscriptionRequestData() : base(MessageTypeEnum.EventSubscriptionRequest) { }

        public class ModelConfigChangedEventSubscriptionRequestData : VTSEventSubscriptonRequestData { }
    }

    /// <summary>
    /// A container for providing subscription options for a Model Config Changed Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified</a>
    /// </summary>
    public class VTSModelConfigChangedEventConfigOptions : VTSEventConfigData
    {
        public VTSModelConfigChangedEventConfigOptions() { }
    }

    public class VTSModelConfigChangedEventData : VTSEventData
    {
        public VTSModelConfigChangedEventData() : base(MessageTypeEnum.ModelConfigChangedEvent) { }

        public class ModelConfigChangedEventData
        {
            public string ModelID;
            public string ModelName;
            public bool HotkeyConfigChanged;
        }
    }

    // Model Moved Event

    public class VTSModelMovedEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSModelMovedEventSubscriptionRequestData() : base(MessageTypeEnum.EventSubscriptionRequest) { }


        public class ModelMovedEventSubscriptionRequestData : VTSEventSubscriptonRequestData { }
    }

    /// <summary>
    /// A container for providing subscription options for a Model Config Changed Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated</a>
    /// </summary>
    public class VTSModelMovedEventConfigOptions : VTSEventConfigData
    {
        public VTSModelMovedEventConfigOptions() { }
    }

    public class VTSModelMovedEventData : VTSEventData
    {
        public VTSModelMovedEventData() : base(MessageTypeEnum.ModelMovedEvent) { }


        public class ModelMovedEventData
        {
            public string ModelID;
            public string ModelName;
            public ModelPosition ModelPosition;
        }
    }

    // Model Outline Event

    public class VTSModelOutlineEventSubscriptionRequestData : VTSEventSubscriptionRequestData
    {
        public VTSModelOutlineEventSubscriptionRequestData() : base(MessageTypeEnum.EventSubscriptionRequest) { }

        public class ModelOutlineEventSubscriptionRequestData : VTSEventSubscriptonRequestData { }
    }

    /// <summary>
    /// A container for providing subscription options for a Model Outline Event subscription.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed</a>
    /// </summary>
    public class VTSModelOutlineEventConfigOptions : VTSEventConfigData
    {
        public VTSModelOutlineEventConfigOptions() { }

        public bool Draw;
    }

    public class VTSModelOutlineEventData : VTSEventData
    {
        public VTSModelOutlineEventData() : base(MessageTypeEnum.ModelOutlineEvent) { }


        public class ModelOutlineEventData
        {
            public string ModelID;
            public string ModelName;
            public Pair[] ConvexHull;
            public Pair ConvexHullCenter;
            public Pair WindowSize;
        }
    }

    #endregion

    #region Helper Classes
    public class DTO
    {
    }

    public struct Pair
    {
        public float X;
        public float Y;
    }

    #region Model Data
    public class ModelData : DTO
    {
        public bool ModelLoaded;
        public string ModelName;
        public string ModelID;
    }

    public class VTSModelData : ModelData
    {
        public string VTSModelName;
        public string VTSModelIconName;
    }

    public class ModelPosition : DTO
    {
        public float PositionX;
        public float PositionY;
        public float Rotation;
        public float Size;
    }
    #endregion

    #region ColorTint
    // must be from 1-255
    public class ColorTint
    {
        public byte ColorR;
        public byte ColorG;
        public byte ColorB;
        public byte ColorA;
    }

    public class ArtMeshColorTint : ColorTint
    {
        public ArtMeshColorTint(ColorTint tint)
        {
            ColorA = tint.ColorA;
            ColorB = tint.ColorB;
            ColorG = tint.ColorG;
            ColorR = tint.ColorR;
        }

        public float MixWithSceneLightingColor = 1.0f;
    }

    public class ArtMeshMatcher
    {
        public bool TintAll = true;
        public int[] ArtMeshNumber;
        public string[] NameExact;
        public string[] NameContains;
        public string[] TagExact;
        public string[] TagContains;
    }

    public class ColorCapturePart : ColorTint
    {
        public bool Active;
    }
    #endregion

    public class VTSParameter : DTO
    {
        public string Name;
        public string AddedBy;
        public float Value;
        public float Min;
        public float Max;
        public float DefaultValue;
    }

    public class HotkeyData : DTO
    {
        public string Name;
        public HotkeyAction Type;
        public string File;
        public string HotkeyID;
    }

    public class VTSCustomParameter : DTO
    {
        // 4-32 characters, alphanumeric
        public string ParameterName;
        public string Explanation;
        public float Min;
        public float Max;
        public float DefaultValue;
    }

    public class VTSParameterInjectionValue
    {
        public string Id;
        public float Value;
        public float Weight;
    }

    public class ExpressionData
    {
        public string Name;
        public string File;
        public bool Active;
        public bool DeactivateWhenKeyIsLetGo;
        public bool AutoDeactivateAfterSeconds;
        public float SecondsRemaining;
        public HotkeyData[] UsedInHotkeys;
        public VTSParameter[] Parameters;
    }

    public class VTSPhysicsGroup
    {
        public string GroupID;
        public string GroupName;
        public float StrengthMultiplier;
        public float WindMultiplier;
    }

    public class VTSPhysicsOverride
    {
        public string Id;
        public float Value;
        public bool SetBaseValue;
        public float OverrideSeconds;
    }

    /// <summary>
    /// A container for holding the numerous retrieval options for an Item List request.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene</a>
    /// </summary>
    public class VTSItemListOptions
    {
        public VTSItemListOptions() { }

        public VTSItemListOptions(
            bool includeAvailableSpots,
            bool includeItemInstancesInScene,
            bool includeAvailableItemFiles,
            string onlyItemsWithFileName,
            string onlyItemsWithInstanceID
        )
        {
            IncludeAvailableSpots = includeAvailableSpots;
            IncludeItemInstancesInScene = includeItemInstancesInScene;
            IncludeAvailableItemFiles = includeAvailableItemFiles;
            OnlyItemsWithFileName = onlyItemsWithFileName;
            OnlyItemsWithInstanceID = onlyItemsWithInstanceID;
        }

        public bool IncludeAvailableSpots;
        public bool IncludeItemInstancesInScene;
        public bool IncludeAvailableItemFiles;
        public string OnlyItemsWithFileName;
        public string OnlyItemsWithInstanceID;
    }

    public class ItemInstance
    {
        public string FileName;
        public string InstanceID;
        public int Order;
        public string Type;
        public bool Censored;
        public bool Flipped;
        public bool Locked;
        public float Smoothing;
        public float Framerate;
        public int FrameCount;
        public int CurrentFrame;
        public bool PinnedToModel;
        public string PinnedModelID;
        public string PinnedArtMeshID;
        public string GroupName;
        public string SceneName;
        public bool FromWorkshop;
    }

    public class ItemFile
    {
        public string FileName;
        public string Type;
        public int LoadedCount;
    }

    /// <summary>
    /// A container for holding the numerous loading options for an Item Load request.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene">https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene</a>
    /// </summary>
    public class VTSItemLoadOptions
    {
        public VTSItemLoadOptions() // TODO remove magic numbers
        {
            Size = 0.32f;
            Order = 1;
            UnloadWhenPluginDisconnects = true;
        }

        public VTSItemLoadOptions(
            float positionX,
            float positionY,
            float size,
            float rotation,
            float fadeTime,
            int order,
            bool failIfOrderTaken,
            float smoothing,
            bool censored,
            bool flipped,
            bool locked,
            bool unloadWhenPluginDisconnects
        )
        {
            PositionX = positionX;
            PositionY = positionY;
            Size = size;
            Rotation = rotation;
            FadeTime = fadeTime;
            Order = order;
            FailIfOrderTaken = failIfOrderTaken;
            Smoothing = smoothing;
            Censored = censored;
            Flipped = flipped;
            Locked = locked;
            UnloadWhenPluginDisconnects = unloadWhenPluginDisconnects;
        }

        public float PositionX;
        public float PositionY;
        public float Size;
        public float Rotation;
        public float FadeTime;
        public int Order;
        public bool FailIfOrderTaken;
        public float Smoothing;
        public bool Censored;
        public bool Flipped;
        public bool Locked;
        public bool UnloadWhenPluginDisconnects;
    }

    /// <summary>
    /// A container for holding the numerous unloading options for an Item Unload request.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene">https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene</a>
    /// </summary>
    public class VTSItemUnloadOptions
    {
        public VTSItemUnloadOptions()
        {
        }

        public VTSItemUnloadOptions(
            string[] itemInstanceIDs,
            string[] fileNames,
            bool unloadAllInScene,
            bool unloadAllLoadedByThisPlugin,
            bool allowUnloadingItemsLoadedByUserOrOtherPlugins
        )
        {
            ItemInstanceIDs = itemInstanceIDs;
            FileNames = fileNames;
            UnloadAllInScene = unloadAllInScene;
            UnloadAllLoadedByThisPlugin = unloadAllLoadedByThisPlugin;
            AllowUnloadingItemsLoadedByUserOrOtherPlugins = allowUnloadingItemsLoadedByUserOrOtherPlugins;
        }

        public string[] ItemInstanceIDs;
        public string[] FileNames;
        public bool UnloadAllInScene;
        public bool UnloadAllLoadedByThisPlugin;
        public bool AllowUnloadingItemsLoadedByUserOrOtherPlugins;
    }

    public class UnloadedItem
    {
        public string InstanceID;
        public string FileName;
    }

    /// <summary>
    /// A container for holding the numerous animation options for an Item Animation Control request.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations">https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations</a>
    /// </summary>
    public class VTSItemAnimationControlOptions
    {
        public VTSItemAnimationControlOptions() // @TODO Look Into -1, remove magic numbers
        {
            Framerate = -1;
            Frame = -1;
            Brightness = -1;
            Opacity = -1;
        }

        public VTSItemAnimationControlOptions(
            int framerate,
            int frame,
            float brightness,
            float opacity,
            bool setAutoStopFrames,
            int[] autoStopFrames,
            bool setAnimationPlayState,
            bool animationPlayState
        )
        {
            Framerate = framerate;
            Frame = frame;
            Brightness = brightness;
            Opacity = opacity;
            SetAutoStopFrames = setAutoStopFrames;
            AutoStopFrames = autoStopFrames;
            SetAnimationPlayState = setAnimationPlayState;
            AnimationPlayState = animationPlayState;
        }

        public int Framerate;
        public int Frame;
        public float Brightness;
        public float Opacity;
        public bool SetAutoStopFrames;
        public int[] AutoStopFrames;
        public bool SetAnimationPlayState;
        public bool AnimationPlayState;
    }

    /// <summary>
    /// A container for holding the numerous movement options for an Item Move request.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene">https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene</a>
    /// </summary>
    public class VTSItemMoveOptions
    {
        public VTSItemMoveOptions()
        {
            PositionX = -1000; // TODO remove magic numbers
            PositionY = -1000;
            Size = -1000;
            Rotation = -1000;
            Order = -1000;
        }

        public VTSItemMoveOptions(
            float timeInSeconds,
            VTSItemMotionCurve fadeMode,
            float positionX,
            float positionY,
            float size,
            float rotation,
            int order,
            bool setFlip,
            bool flip,
            bool userCanStop
        )
        {
            TimeInSeconds = timeInSeconds;
            FadeMode = fadeMode;
            PositionX = positionX;
            PositionY = positionY;
            Size = size;
            Rotation = rotation;
            Order = order;
            SetFlip = setFlip;
            Flip = flip;
            UserCanStop = userCanStop;
        }

        public float TimeInSeconds;
        public VTSItemMotionCurve FadeMode;
        public float PositionX;
        public float PositionY;
        public int Order;
        public float Size;
        public float Rotation;
        public bool SetFlip;
        public bool Flip;
        public bool UserCanStop;
    }

    /// <summary>
    /// A container for linking an Item Instance ID to its corresponding options for an Item Move request.
    /// 
    /// For more info about what each field does, see 
    /// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene">https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene</a>
    /// </summary>
    public struct VTSItemMoveEntry
    {
        public VTSItemMoveEntry(string itemInsanceID, VTSItemMoveOptions options)
        {
            ItemInsanceID = itemInsanceID;
            Options = options;
        }

        public string ItemInsanceID;
        public VTSItemMoveOptions Options;
    }

    public struct VTSItemToMove
    {
        public VTSItemToMove(
            string itemInstanceID,
            float timeInSeconds,
            string fadeMode,
            float positionX,
            float positionY,
            float size,
            float rotation,
            int order,
            bool setFlip,
            bool flip,
            bool userCanStop
        )
        {
            ItemInstanceID = itemInstanceID;
            TimeInSeconds = timeInSeconds;
            FadeMode = fadeMode;
            PositionX = positionX;
            PositionY = positionY;
            Size = size;
            Rotation = rotation;
            Order = order;
            SetFlip = setFlip;
            Flip = flip;
            UserCanStop = userCanStop;
        }

        public string ItemInstanceID;
        public float TimeInSeconds;
        public string FadeMode;
        public float PositionX;
        public float PositionY;
        public int Order;
        public float Size;
        public float Rotation;
        public bool SetFlip;
        public bool Flip;
        public bool UserCanStop;
    }

    public struct MovedItem
    {
        public string ItemInstanceID;
        public bool Success;
        public ErrorID ErrorID;
    }
    #endregion
}
