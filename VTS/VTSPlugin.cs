using System.Text.RegularExpressions;
using VTS.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using static VTS.VTSAuthTokenRequestData;
using static VTS.VTSModelLoadRequestData;
using static VTS.VTSMoveModelRequestData;
using static VTS.VTSHotkeysInCurrentModelRequestData;
using static VTS.VTSHotkeyTriggerRequestData;
using static VTS.VTSColorTintRequestData;
using static VTS.VTSParameterValueRequestData;
using static VTS.VTSParameterCreationRequestData;
using static VTS.VTSParameterDeletionRequestData;
using static VTS.VTSInjectParameterDataRequestData;
using static VTS.VTSExpressionActivationRequestData;
using static VTS.VTSSetCurrentModelPhysicsRequestData;
using static VTS.VTSItemListRequestData;
using static VTS.VTSItemLoadRequestData;
using static VTS.VTSItemUnloadRequestData;
using static VTS.VTSItemAnimationControlRequestData;
using static VTS.VTSItemMoveRequestData;
using static VTS.VTSArtMeshSelectionRequestData;
using static VTS.VTSEventSubscriptionRequestData;

namespace VTS
{
    /// <summary>
    /// The base class for VTS plugin creation.
    /// </summary>
    public class VTSPlugin
    {
        #region Properties

        protected string _pluginName = "ExamplePlugin";
        /// <summary>
        /// The name of this plugin. Required for authorization purposes..
        /// </summary>
        /// <value></value>
        public string PluginName { get { return _pluginName; } }

        protected string _pluginAuthor = "ExampleAuthor";
        /// <summary>
        /// The name of this plugin's author. Required for authorization purposes.
        /// </summary>
        /// <value></value>
        public string PluginAuthor { get { return _pluginAuthor; } }

        protected string _pluginIcon = null;
        /// <summary>
        /// The icon for this plugin.
        /// Base 64 string PNG/JPG
        /// </summary>
        /// <value></value>
        public string PluginIcon { get { return _pluginIcon; } }

        /// <summary>
        /// The underlying WebSocket for connecting to VTS.
        /// </summary>
        /// <value></value>
        protected VTSWebSocket Socket { get; private set; } = new VTSWebSocket();

        private string _token = null;

        /// <summary>
        /// The underlying Token Storage mechanism for connecting to VTS.
        /// </summary>
        /// <value></value>
        protected ITokenStorage TokenStorage { get; private set; } = null;

        /// <summary>
        /// Is the plugin currently authenticated?
        /// </summary>
        /// <value></value>
        public bool IsAuthenticated { get; private set; } = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Selects the Websocket, JSON utility, and Token Storage implementations, then attempts to Authenticate the plugin.
        /// </summary>
        /// <param name="webSocket">The websocket implementation.</param>
        /// <param name="tokenStorage">The Token Storage implementation.</param>
        /// <param name="onConnect">Callback executed upon successful initialization.</param>
        /// <param name="onDisconnect">Callback executed upon disconnecting from VTS.</param>
        /// <param name="onError">The Callback executed upon failed initialization.</param>
        public void Initialize(IWebSocket webSocket, ITokenStorage tokenStorage, Action onConnect, Action onDisconnect, Action onError)
        {
            TokenStorage = tokenStorage;
            Socket.Initialize(webSocket);

            void OnCombinedConnect()
            {
                Socket.ResubscribeToEvents();
                onConnect();
            }

            Socket.Connect(() =>
                // If API enabled, authenticate
                Authenticate(
                    (r) =>
                    {
                        if (!r.Data.Authenticated)
                        {
                            Reauthenticate(OnCombinedConnect, onError);
                        }
                        else
                        {
                            IsAuthenticated = true;
                            OnCombinedConnect();
                        }
                    },
                    (r) => Reauthenticate(OnCombinedConnect, onError)),
            () =>
            {
                IsAuthenticated = false;
                onDisconnect();
            },
            () =>
            {
                IsAuthenticated = false;
                onError();
            });
        }

        /// <summary>
        /// Disconnects from VTube Studio. Will fire the onDisconnect callback set via the Initialize method.
        /// </summary>
        public void Disconnect()
        {
            Socket?.Disconnect();
        }

        #endregion

        #region Authentication

        private void Authenticate(Action<VTSAuthTokenRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            IsAuthenticated = false;

            if (TokenStorage != null)
            {
                _token = TokenStorage.LoadToken();

                if (string.IsNullOrEmpty(_token))
                {
                    GetToken(onSuccess, onError);
                }
                else
                {
                    UseToken(onSuccess, onError);
                }
            }
            else
            {
                GetToken(onSuccess, onError);
            }
        }

        private void Reauthenticate(Action onConnect, Action onError)
        {
            IsAuthenticated = false;
            TokenStorage.DeleteToken();

            Authenticate(
                (t) =>
                {
                    IsAuthenticated = true;
                    onConnect();
                },
                (t) =>
                {
                    IsAuthenticated = false;
                    onError();
                }
            );
        }

        private void GetToken(Action<VTSAuthTokenRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSAuthTokenRequestData tokenRequest = new VTSAuthTokenRequestData()
            {
                Data = new AuthRequestData()
                {
                    PluginName = _pluginName,
                    PluginDeveloper = _pluginAuthor,
                    PluginIcon = _pluginIcon
                }
            };

            Socket.Send<VTSAuthTokenRequestData, VTSAuthTokenRequestData>(tokenRequest,
            (a) =>
            {
                _token = a.Data.AuthenticationToken;
                TokenStorage?.SaveToken(_token);
                UseToken(onSuccess, onError);
            },
            onError);
        }

        private void UseToken(Action<VTSAuthTokenRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSAuthTokenRequestData authRequest = new VTSAuthTokenRequestData
            {
                Data = new AuthRequestData()
                {
                    PluginName = _pluginName,
                    PluginDeveloper = _pluginAuthor,
                    AuthenticationToken = _token
                }
            };

            Socket.Send(authRequest, onSuccess, onError);
        }

        #endregion

        #region Port Discovery

        /// <summary>
        /// Gets a dictionary indexed by port number containing information about all available VTube Studio ports.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#api-server-discovery-udp">https://github.com/DenchiSoft/VTubeStudio#api-server-discovery-udp</a>
        /// </summary>
        /// <returns>Dictionary indexed by port number.</returns>
        public Dictionary<int, VTSVTubeStudioAPIStateBroadcastData> GetPorts()
        {
            return Socket.GetPorts();
        }

        /// <summary>
        /// Sets the connection port to the given number. Returns true if the port is a valid VTube Studio port, returns false otherwise. 
        /// If the port number is changed while an active connection exists, you will need to reconnect.
        /// </summary>
        /// <param name="port">The port to connect to.</param>
        /// <returns>True if the port is a valid VTube Studio port, False otherwise.</returns>
        public bool SetPort(int port)
        {
            return Socket.SetPort(port);
        }

        /// <summary>
        /// Sets the connection IP address to the given string. Returns true if the string is a valid IP Address format, returns false otherwise.
        /// If the IP Address is changed while an active connection exists, you will need to reconnect.
        /// </summary>
        /// <param name="ipString">The string form of the IP address, in dotted-quad notation for IPv4.</param>
        /// <returns>True if the string is a valid IP Address format, False otherwise.</returns>
        public bool SetIPAddress(string ipString)
        {
            return Socket.SetIPAddress(ipString);
        }

        #endregion

        #region VTS General API Wrapper

        /// <summary>
        /// Gets the current state of the VTS API.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#status">https://github.com/DenchiSoft/VTubeStudio#status</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetAPIState(Action<VTSStateRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSStateRequestData request = new VTSStateRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets current metrics about the VTS application.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-current-vts-statistics">https://github.com/DenchiSoft/VTubeStudio#getting-current-vts-statistics</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetStatistics(Action<VTSStatisticsRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSStatisticsRequestData request = new VTSStatisticsRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets the list of VTS folders.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-list-of-vts-folders">https://github.com/DenchiSoft/VTubeStudio#getting-list-of-vts-folders</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetFolderInfo(Action<VTSFolderInfoRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSFolderInfoRequestData request = new VTSFolderInfoRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets information about the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-the-currently-loaded-model">https://github.com/DenchiSoft/VTubeStudio#getting-the-currently-loaded-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetCurrentModel(Action<VTSCurrentModelRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSCurrentModelRequestData request = new VTSCurrentModelRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets the list of all available VTS models.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-a-list-of-available-vts-models">https://github.com/DenchiSoft/VTubeStudio#getting-a-list-of-available-vts-models</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetAvailableModels(Action<VTSAvailableModelsRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSAvailableModelsRequestData request = new VTSAvailableModelsRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Loads a VTS model by its Model ID. Will return an error if the model cannot be loaded.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-a-vts-model-by-its-id">https://github.com/DenchiSoft/VTubeStudio#loading-a-vts-model-by-its-id</a>
        /// </summary>
        /// <param name="modelID">The Model ID/Name.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void LoadModel(string modelID, Action<VTSModelLoadRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSModelLoadRequestData request = new VTSModelLoadRequestData()
            {
                Data = new ModelLoadRequestData()
                {
                    ModelID = modelID
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Moves the currently loaded VTS model.
        /// 
        /// For more info, particularly about what each position value field does, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-the-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#moving-the-currently-loaded-vts-model</a>
        /// </summary>
        /// <param name="position">The desired position information. Fields will be null-valued by default.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void MoveModel(MoveModelRequestData position, Action<VTSMoveModelRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSMoveModelRequestData request = new VTSMoveModelRequestData
            {
                Data = position
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of available hotkeys.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model</a>
        /// </summary>
        /// <param name="modelID">Optional, the model ID to get hotkeys for.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSHotkeysInCurrentModelRequestData request = new VTSHotkeysInCurrentModelRequestData()
            {
                Data = new HotkeysInCurrentModelRequestData()
                {
                    ModelID= modelID
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of available hotkeys for the specified Live2D Item.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model</a>
        /// </summary>
        /// <param name="live2DItemFileName">Optional, the Live 2D item to get hotkeys for.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSHotkeysInCurrentModelRequestData request = new VTSHotkeysInCurrentModelRequestData()
            {
                Data = new HotkeysInCurrentModelRequestData()
                {
                    Live2DItemFileName = live2DItemFileName
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Triggers a given hotkey.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys">https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys</a>
        /// </summary>
        /// <param name="hotkeyID">The model ID to get hotkeys for.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSHotkeyTriggerRequestData request = new VTSHotkeyTriggerRequestData()
            {
                Data = new HotkeyTriggerRequestData()
                {
                    HotkeyID = hotkeyID
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Triggers a given hotkey on a specified Live2D item.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys">https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys</a>
        /// </summary>
        /// <param name="itemInstanceID">The instance ID of the Live2D item.</param>
        /// <param name="hotkeyID">The model ID to get hotkeys for.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSHotkeyTriggerRequestData request = new VTSHotkeyTriggerRequestData()
            {
                Data = new HotkeyTriggerRequestData()
                {
                    HotkeyID = hotkeyID,
                    ItemInstanceID = itemInstanceID
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of all available art meshes in the current VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-artmeshes-in-current-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-artmeshes-in-current-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetArtMeshList(Action<VTSArtMeshListRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSArtMeshListRequestData request = new VTSArtMeshListRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Tints matched components of the current art mesh.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#tint-artmeshes-with-color">https://github.com/DenchiSoft/VTubeStudio#tint-artmeshes-with-color</a>
        /// </summary>
        /// <param name="tint">The tint to be applied.</param>
        /// <param name="mixWithSceneLightingColor"> The amount to mix the color with scene lighting, from 0 to 1. Default is 1.0, which will have the color override scene lighting completely.
        /// <param name="matcher">The ArtMesh matcher search parameters.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSColorTintRequestData request = new VTSColorTintRequestData()
            {
                Data = new ColorTintRequestData()
                {
                    ArtMeshMatcher = matcher,
                    ColorTint = new ArtMeshColorTint(tint)
                    {
                        MixWithSceneLightingColor = Math.Min(1, Math.Max(mixWithSceneLightingColor, 0))
                    },
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets color information about the scene lighting overlay, if it is enabled.
        /// 
        /// For more info, see
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-scene-lighting-overlay-color">https://github.com/DenchiSoft/VTubeStudio#getting-scene-lighting-overlay-color</a>
        /// </summary>
        /// <param name="onSuccess"></param>
        /// <param name="onError"></param>
        public void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayInfoRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSSceneColorOverlayInfoRequestData request = new VTSSceneColorOverlayInfoRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Checks to see if a face is being tracked.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#checking-if-face-is-currently-found-by-tracker">https://github.com/DenchiSoft/VTubeStudio#checking-if-face-is-currently-found-by-tracker</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetFaceFound(Action<VTSFaceFoundRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSFaceFoundRequestData request = new VTSFaceFoundRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of input parameters for the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetInputParameterList(Action<VTSInputParameterListRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSInputParameterListRequestData request = new VTSInputParameterListRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets the value for the specified parameter.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-one-specific-parameter-default-or-custom">https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-one-specific-parameter-default-or-custom</a>
        /// </summary>
        /// <param name="parameterName">The name of the parameter to get the value of.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetParameterValue(string parameterName, Action<VTSParameterValueRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSParameterValueRequestData request = new VTSParameterValueRequestData()
            {
                Data = new ParameterValueRequestData()
                {
                    Name = parameterName
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of input parameters for the currently loaded Live2D model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-all-live2d-parameters-in-the-current-model">https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-all-live2d-parameters-in-the-current-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetLive2DParameterList(Action<VTSLive2DParameterListRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSLive2DParameterListRequestData request = new VTSLive2DParameterListRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Adds a custom parameter to the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#adding-new-tracking-parameters-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#adding-new-tracking-parameters-custom-parameters</a>
        /// </summary>
        /// <param name="parameter">Information about the parameter to add. Parameter name must be 4-32 characters, alphanumeric.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSParameterCreationRequestData request = new VTSParameterCreationRequestData()
            {
                Data = new ParameterCreationRequestData()
                {
                    ParameterName = SanitizeParameterName(parameter.ParameterName),
                    Explanation = parameter.Explanation,
                    Min = parameter.Min,
                    Max = parameter.Max,
                    DefaultValue = parameter.DefaultValue
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Removes a custom parameter from the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#delete-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#delete-custom-parameters</a>
        /// </summary>
        /// <param name="parameterName">The name f the parameter to remove.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSParameterDeletionRequestData request = new VTSParameterDeletionRequestData()
            {
                Data = new ParameterDeletionRequestData()
                {
                    ParameterName = SanitizeParameterName(parameterName)
                }
            };
           
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Sends a list of parameter names and corresponding values to assign to them.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
        /// </summary>
        /// <param name="values">A list of parameters and the values to assign to them.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterDataRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            InjectParameterValues(values, VTSInjectParameterMode.SET, false, onSuccess, onError);
        }

        /// <summary>
        /// Sends a list of parameter names and corresponding values to assign to them.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
        /// </summary>
        /// <param name="values">A list of parameters and the values to assign to them.</param>
        /// <param name="mode">The method by which the parameter values are applied.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterDataRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            InjectParameterValues(values, mode, false, onSuccess, onError);
        }

        /// <summary>
        /// Sends a list of parameter names and corresponding values to assign to them.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
        /// </summary>
        /// <param name="values">A list of parameters and the values to assign to them.</param>
        /// <param name="mode">The method by which the parameter values are applied.</param>
        /// <param name="faceFound">A flag which can be set to True to tell VTube Studio to consider the user face as found.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound, Action<VTSInjectParameterDataRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            foreach (VTSParameterInjectionValue value in values)
            {
                value.Id = SanitizeParameterName(value.Id);
            }

            var request = new VTSInjectParameterDataRequestData()
            {
                Data = new InjectParameterDataRequestData()
                {

                    FaceFound = faceFound,
                    ParameterValues = values,
                    Mode = InjectParameterModeToString(mode)
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Requests a list of the states of all expressions in the currently loaded model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-current-expression-state-list">https://github.com/DenchiSoft/VTubeStudio#requesting-current-expression-state-list</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetExpressionStateList(Action<VTSExpressionStateRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSExpressionStateRequestData request = new VTSExpressionStateRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Activates or deactivates the given expression.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-activation-or-deactivation-of-expressions">https://github.com/DenchiSoft/VTubeStudio#requesting-activation-or-deactivation-of-expressions</a>
        /// </summary>
        /// <parame name="expression">The expression file name to change the state of.</param>
        /// <param name="active">The state to set the expression to. True to activate, false to deactivate.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSExpressionActivationRequestData request = new VTSExpressionActivationRequestData()
            {
                Data = new ExpressionActivationRequestData()
                {
                    ExpressionFile = expression,
                    Active = active
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets physics information about the currently loaded model.
        /// 
        /// For more info, see
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-physics-settings-of-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#getting-physics-settings-of-currently-loaded-vts-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetCurrentModelPhysics(Action<VTSGetCurrentModelPhysicsRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSGetCurrentModelPhysicsRequestData request = new VTSGetCurrentModelPhysicsRequestData();
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Overrides the physics properties of the current model. Once a plugin has overridden a model's physics, no other plugins may do so.
        /// 
        /// For more info, see
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#overriding-physics-settings-of-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#overriding-physics-settings-of-currently-loaded-vts-model</a>
        /// </summary>
        /// <param name="strengthOverrides">A list of strength override settings </param>
        /// <param name="windOverrides">A list of wind override settings.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSSetCurrentModelPhysicsRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSSetCurrentModelPhysicsRequestData request = new VTSSetCurrentModelPhysicsRequestData()
            {
                Data = new SetCurrentModelPhysicsRequestData
                {
                    StrengthOverrides = strengthOverrides,
                    WindOverrides = windOverrides
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Changes the NDI configuration.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#get-and-set-ndi-settings">https://github.com/DenchiSoft/VTubeStudio#get-and-set-ndi-settings</a>
        /// </summary>
        /// <parame name="config">The desired NDI configuration.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SetNDIConfig(VTSNDIConfigRequestData config, Action<VTSNDIConfigRequestData> onSuccess, Action<VTSErrorData> onError)
        {
            Socket.Send(config, onSuccess, onError);
        }

        /// <summary>
        /// Retrieves a list of items, either in the scene or available as files, based on the provided options.
        /// 
        /// For more, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene</a>
        /// </summary>
        /// <param name="options">Configuration options about the request.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSItemListRequestData request = new VTSItemListRequestData()
            {
                Data = new ItemListRequestData
                {
                    IncludeAvailableSpots = options.IncludeAvailableSpots,
                    IncludeItemInstancesInScene = options.IncludeItemInstancesInScene,
                    IncludeAvailableItemFiles = options.IncludeAvailableItemFiles,
                    OnlyItemsWithFileName = options.OnlyItemsWithFileName,
                    OnlyItemsWithInstanceID = options.OnlyItemsWithInstanceID
                }
            };
           
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Loads an item into the scene, with properties based on the provided options.
        /// 
        /// For more, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene">https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene</a>
        /// </summary>
        /// <param name="fileName">The file name of the item to load, typically retrieved from an ItemListRequest.</param>
        /// <param name="options">Configuration options about the request.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void LoadItem(string fileName, VTSItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSItemLoadRequestData request = new VTSItemLoadRequestData()
            {
                Data = new ItemLoadRequestData
                {
                    FileName = fileName,
                    PositionX = options.PositionX,
                    PositionY = options.PositionY,
                    Size = options.Size,
                    Rotation = options.Rotation,
                    FadeTime = options.FadeTime,
                    Order = options.Order,
                    FailIfOrderTaken = options.FailIfOrderTaken,
                    Smoothing = options.Smoothing,
                    Censored = options.Censored,
                    Flipped = options.Flipped,
                    Locked = options.Locked,
                    UnloadWhenPluginDisconnects = options.UnloadWhenPluginDisconnects
                }
            };
            
            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Unload items from the scene, either broadly, by identifier, or by file name, based on the provided options.
        /// 
        /// For more, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene">https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene</a>
        /// </summary>
        /// <param name="options">Configuration options about the request.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSItemUnloadRequestData request = new VTSItemUnloadRequestData()
            {
                Data = new ItemUnloadRequestData
                {
                    InstanceIDs = options.ItemInstanceIDs,
                    FileNames = options.FileNames,
                    UnloadAllInScene = options.UnloadAllInScene,
                    UnloadAllLoadedByThisPlugin = options.UnloadAllLoadedByThisPlugin,
                    AllowUnloadingItemsLoadedByUserOrOtherPlugins = options.AllowUnloadingItemsLoadedByUserOrOtherPlugins
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Alters the properties of the item of the specified ID based on the provided options.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations">https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations</a>
        /// <param name="itemInstanceID">The ID of the item to move.</param>
        /// <param name="options">Configuration options about the request.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void AnimateItem(string itemInstanceID, VTSItemAnimationControlOptions options, Action<VTSItemAnimationControlResponseData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSItemAnimationControlRequestData request = new VTSItemAnimationControlRequestData()
            {
                Data = new ItemAnimationControlRequestData()
                {
                    ItemInstanceID = itemInstanceID,
                    Framerate = options.Framerate,
                    Frame = options.Frame,
                    Brightness = options.Brightness,
                    Opacity = options.Opacity,
                    SetAutoStopFrames = options.SetAutoStopFrames,
                    AutoStopFrames = options.AutoStopFrames,
                    SetAnimationPlayState = options.SetAnimationPlayState,
                    AnimationPlayState = options.AnimationPlayState
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        /// <summary>
        /// Moves the items of the specified IDs based on their provided options.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene">https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene</a>
        /// </summary>
        /// <param name="items">The list of Item Insance IDs and their corresponding movement options</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void MoveItem(VTSItemMoveEntry[] items, Action<VTSItemMoveResponseData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSItemMoveRequestData request = new VTSItemMoveRequestData()
            {
                Data = new ItemMoveRequestData()
                {
                    ItemsToMove = items.Select(entry => new VTSItemToMove(
                            entry.ItemInsanceID,
                            entry.Options.TimeInSeconds,
                            MotionCurveToString(entry.Options.FadeMode),
                            entry.Options.PositionX,
                            entry.Options.PositionY,
                            entry.Options.Size,
                            entry.Options.Rotation,
                            entry.Options.Order,
                            entry.Options.SetFlip,
                            entry.Options.Flip,
                            entry.Options.UserCanStop)).ToArray()
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        public void RequestArtMeshSelection(string textOverride, string helpOverride, int count,
            ICollection<string> activeArtMeshes,
            Action<VTSArtMeshSelectionResponseData> onSuccess, Action<VTSErrorData> onError)
        {
            VTSArtMeshSelectionRequestData request = new VTSArtMeshSelectionRequestData()
            {
                Data = new ArtMeshSelectionRequestData()
                {
                    TextOverride = textOverride,
                    HelpOverride = helpOverride,
                    RequestedArtMeshCount = count,
                    ActiveArtMeshes = activeArtMeshes.ToArray()
                }
            };

            Socket.Send(request, onSuccess, onError);
        }

        #endregion

        #region VTS Event Subscription API Wrapper

        private void SubscribeToEvent<T, K>(string eventName, bool subscribed, VTSEventConfigData config, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) where T : VTSEventSubscriptionRequestData, new() where K : VTSEventData
        {
            var request = new VTSEventSubscriptionRequestData()
            {
                Data = new VTSEventSubscriptonRequestData()
                {
                    EventName = eventName,
                    Subscribe = subscribed,
                    Config= config
                }
            };

            Socket.SendEventSubscription(request, onEvent, onSubscribe, onError, () =>
            {
                SubscribeToEvent<T, K>(eventName, subscribed, config, onEvent, onSubscribe, onError);
            });
        }

        /// <summary>
        /// Unsubscribes from all events.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData>(null, false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Test Event for testing the event API. Can be configured with a message to echo back every second.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event</a>
        /// </summary>
        /// <param name="config">Configuration options about the subscription.</param>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData>("TestEvent", true, config, onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Test Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData>("TestEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Model Loaded Event. Can be configured with a model ID to only recieve events about the given model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded</a>
        /// </summary>
        /// <param name="config">Configuration options about the subscription.</param>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSModelLoadedEventData>("ModelLoadedEvent", true, config, onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Model Loaded Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSTestEventData>("ModelLoadedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Tracking Status Changed Event.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking</a>
        /// </summary>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData>("TrackingStatusChangedEvent", true, new VTSTrackingEventConfigOptions(), onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Tracking Status Changed Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData>("TrackingStatusChangedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Background Changed Event.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking</a>
        /// </summary>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData>("BackgroundChangedEvent", true, new VTSBackgroundChangedEventConfigOptions(), onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Background Changed Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData>("BackgroundChangedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Model Config Changed Event.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified</a>
        /// </summary>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData>("ModelConfigChangedEvent", true, new VTSModelConfigChangedEventConfigOptions(), onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Model Config Changed Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData>("ModelConfigChangedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Model Moved Event.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated</a>
        /// </summary>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData>("ModelMovedEvent", true, new VTSModelMovedEventConfigOptions(), onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Model Moved Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData>("ModelMovedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        /// <summary>
        /// Subscribes to the Model Outline Event.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed</a>
        /// </summary>
        /// <param name="config">Configuration options about the subscription.</param>
        /// <param name="onEvent">Callback to execute upon receiving an event.</param>
        /// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData>("ModelOutlineEvent", true, config, onEvent, onSubscribe, onError);
        }

        /// <summary>
        /// Unsubscribes from the Model Outline Event.
        /// </summary>
        /// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
        {
            SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData>("ModelOutlineEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Static VTS API callback method which does nothing. Saves you from needing to make a new inline function each time.
        /// </summary>
        /// <param name="response"></param>
        protected static void DoNothingCallback(VTSEventData response)
        {
            // Do nothing!
        }

        private static readonly Regex ALPHANUMERIC = new Regex(@"\W|");
        private static string SanitizeParameterName(string name)
        {
            // between 4 and 32 chars, alphanumeric, underscores allowed
            string output = name;
            output = ALPHANUMERIC.Replace(output, "");
            output.PadLeft(4, 'X');
            output = output.Substring(0, Math.Min(output.Length, 31));

            return output;

        }

        private static string InjectParameterModeToString(VTSInjectParameterMode mode)
        {
            return mode.GetDescription() ?? VTSInjectParameterMode.SET.GetDescription();
        }

        private static string MotionCurveToString(VTSItemMotionCurve curve)
        {
            return curve.GetDescription() ?? VTSItemMotionCurve.LINEAR.GetDescription();
        }
        #endregion
    }
}
