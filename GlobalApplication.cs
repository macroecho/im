#region Keisoft License
/*
 * =================================================================
 * Copyright(c) 2020 KeiSoft,All Rights Reserved.
 * Author: macro
 * Date: 2020-04-15
 * Version: 6.0.0
 * =================================================================
 */
#endregion

using System;
using System.Collections.Generic;

using Android.App;
using Android.Runtime;

using Keisoft.IM.Client;
using Keisoft.IM.Client.Protocol;
using Keisoft.IM.Client.Chat;
using Keisoft.IM.Client.Chat.Enums;

using Chat.Service;
using Chat.Droid.Utilities;
using Chat.Droid.IMModules;
using Chat.Droid.Controllers.Public;
using Chat.Droid.Infrastructures.Manager;

namespace Chat.Droid
{
    [Application]
    public class GlobalApplication : Application
    {
        private const string LogTag = "GlobalApplication";

        public GlobalApplication()
        {

        }

        public GlobalApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        public override void OnCreate()
        {
            base.OnCreate();

            // 注入全局 Application。
            Global.Init(this);

            //try
            //{
            //    LogUtility.Debug(LogTag, "初始化 IM 组件。");

            //    // 初始化通信模块。
            //    IMService.InitIMClient(new AndroidDeviceProvider(), new IMChatModuleConfig
            //    {
            //        ProcessUploadFile = new AndroidProcessUploadFile()
            //    });

            //    LogUtility.Debug(LogTag, "IM 组件初始化成功。");

            //    // 
            //    IMClient.SetOnReceiveMessage(OnReceiveMessage);
            //}
            //catch (Exception ex)
            //{
            //    LogUtility.Error(LogTag, ex.ToString());
            //}
        }

        private void OnReceiveMessage(List<MessageItem> messageItem)
        {
            foreach (var item in messageItem)
            {
                var subType = (MessageSubTypeEnum)item.SubType;

                if (item.Status != MessageStatus.Cancel && (subType == MessageSubTypeEnum.AudioCallMessage || subType == MessageSubTypeEnum.VideoCallMessage))
                {
                    var timeStamp = DateTimeUtility.GetTimeStamp();

                    // 检测是否已经超过呼叫时间了。
                    if (DateTimeUtility.CompareTimeStamp(timeStamp, item.TimeStamp, AudioVideoCallActivity.WaitAnswerTimeoutMs))
                    {
                        continue;
                    }

                    long rid;
                    int type;

                    if (item.Attachment is AudioCallAttachment audioCallAttachment)
                    {
                        rid = audioCallAttachment.RId;
                        type = AudioVideoCallActivity.AudioAnswerType;
                    }
                    else if (item.Attachment is VideoCallAttachment videoCallAttachment)
                    {
                        rid = videoCallAttachment.RId;
                        type = AudioVideoCallActivity.VideoAnswerType;
                    }
                    else
                    {
                        return;
                    }

                    // 获取用户名称和头像。
                    var ur = new SessionService(new AndroidDeviceProvider()).GetPrivateSessionInfo(item.TargetId);

                    var displayName = item.TargetId.ToString();
                    string headImgUrl = null;
                    string backgroundPath = null;

                    if (ur != null)
                    {
                        displayName = UserRelationService.ToDisplayName(ur.Value.Nickname, ur.Value.RemarkName);
                        headImgUrl = UserFolderConfig.ToLocalProfilePhotoPath(ur.Value.HeadImgUrl);
                        backgroundPath = UserFolderConfig.ToLocalProfilePhotoMinPath(ur.Value.HeadImgUrl);
                    }

                    // 打开视频通话页面。
                    AudioVideoCallActivity.Run
                    (
                        context: this,
                        token: UserSessionManager.Instance.Token.ImId,
                        type: type,
                        rid: rid,
                        displayName: displayName,
                        headImgUrl: headImgUrl,
                        backgroundPath: backgroundPath
                    );

                    AudioVideoCallActivity.SetReturnValue(AudioVideoCallActivityOnCloseListener.ReturnVauleKeyMessageType, (byte)item.Type);
                    AudioVideoCallActivity.SetReturnValue(AudioVideoCallActivityOnCloseListener.ReturnVauleKeyServerId, item.ServerId);
                    AudioVideoCallActivity.SetReturnValue(AudioVideoCallActivityOnCloseListener.ReturnVauleKeyLocalId, item.LocalId);
                    AudioVideoCallActivity.SetReturnValue(AudioVideoCallActivityOnCloseListener.ReturnVauleKeyRId, rid);
                    AudioVideoCallActivity.OnCloseListener = new AudioVideoCallActivityOnCloseListener();

                    // 清理未读数量。
                    IMClient.ClearSessionUnreadAsync(item.TargetId, item.Type).ConfigureAwait(false);

                    break;
                }
            }

        }

    }
}
