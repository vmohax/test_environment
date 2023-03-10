using Avalonia.Threading;
using MusConv.Abstractions;
using MusConv.Abstractions.Extensions;
using MusConv.Lib.Joox.Models;
using MusConv.Sentry;
using MusConv.Shared.SharedAbstractions.Enums;
using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Messages;
using MusConv.ViewModels.Models;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Reload;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using MusConv.ViewModels.ViewModels.WebViewViewModels.AuthHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusConv.ViewModels.ViewModels.WebViewViewModels
{
    public class JooxViewModel : WebViewModelBase
	{
		#region Constructors

        public JooxViewModel(MainViewModelBase m) : base(m, new(() => new JooxAuthHandler()))
        {
            Title = "Joox";
            RegState = RegistrationState.Unlogged;
            Model = new JooxModel();
            SourceType = DataSource.Joox;
            LogoKey = LogoStyleKey.JooxLogo;
            SideLogoKey = LeftSideBarLogoKey.JooxSideLogo;
            Url = Urls.Joox;

			#region Commands

            var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
            };

            var commandPlaylistTracks = new List<Command_TaskItem> (TracksCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
            };

            var commandTracksTab = new List<Command_TaskItem> (TracksTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new ViewArtistCommand(CommandTrack_OpenArtist),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandAlbumsTab = new List<Command_TaskItem> (AlbumsTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandArtistsTab = new List<Command_TaskItem> (ArtistsTabCommandsBase)
            {
                new ViewOnYouTubeCommand(CommandTrack_Open),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
            };

            var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
            {
                new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
                new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
                new ExportAsCommand(Command_Export,CommandTaskType.CommandBar),
            };

			#endregion Commands

			#region TransferTasks

            var playlistsTransfer = new List<TaskBase_TaskItem>
            {
                new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, Transfer_Search, Transfer_Send, m, false)
            };
            var albumsTransfer = new List<TaskBase_TaskItem>
            {
                new AlbumTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleAlbum, TransferAlbum_Search, TransferAlbum_Send, m)
            };
            var tracksTransfer = new List<TaskBase_TaskItem>
            {
                new TrackTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleTrack, Transfer_SearchWithAlbums, TransferTrack_Send,  m, true)
            };
            var artistsTransfer = new List<TaskBase_TaskItem>
            {
                new ArtistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStyleArtist, TransferArtist_Search, TransferArtist_Send, m)
            };

			#endregion TransferTasks

			#region Tabs

            var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistsTransfer, commandPlaylistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Playlists), commandPlaylistTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var tracksTab = new TrackTabViewModelBase(m, AppTabs.AmazonLikedTracks, LwTabIconKey.TrackIcon, 
				tracksTransfer, commandTracksTab,
                new Initial_TaskItem("Reload", Initial_Update_Tracks), EmptyCommandsBase,
                new LogOut_TaskItem("LogOut", Log_Out));

            var albumsTab = new AlbumTabViewModelBase(m, LwTabIconKey.AlbumIcon, 
				albumsTransfer, commandAlbumsTab,
                new Initial_TaskItem("Reload", Initial_Update_Album), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

            var artistsTab = new ArtistTabViewModelBase(m, LwTabIconKey.ArtistIcon, 
				artistsTransfer, commandArtistsTab,
                new Initial_TaskItem("Reload", Initial_Update_Artists), commandTracks,
                new LogOut_TaskItem("LogOut", Log_Out));

			#endregion Tabs

            Tabs.Add(playlistsTab);
            Tabs.Add(albumsTab);
            Tabs.Add(artistsTab);
            Tabs.Add(tracksTab);
        }

		#endregion Constructors

		#region AuthMethods

        public override async Task<bool> IsServiceSelectedAsync()
        {
            try
            {
                MainViewModel.NeedLogin = this;
                await Dispatcher.UIThread.InvokeAsync(NavigateToContent);

                if (!Model.IsAuthenticated())
                {
                    if (SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data)
                        && await IsServiceDataExecuted(data))
                    {
                        return true;
                    }

                    await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                    return false;
                }

                await InitialUpdateForCurrentTab().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
                return false;
            }
        }

        public override async Task<bool> IsServiceDataExecuted(Dictionary<string, List<string>> data)
        {
            var serviceData = data.FirstOrDefault(x => x.Key == Title).Value;
            var credentials = Serializer.Deserialize<JooxCreds>(serviceData.FirstOrDefault());

            if ((await Model.Initialize(credentials)).LoginNavigationState != LoginNavigationState.Done)
            {
                await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                return false;
            }

            if (Accounts.Count == 0)
            {
                LoadUserAccountInfo(serviceData);
            }

            await InitialAuthorization();
            return true;
        }

        public override async Task Web_NavigatingAsync(object s, object t)
        {
            OnLoginPageLeft();

            var creds = s as JooxCreds;

            if ((await Model.Initialize(creds)).LoginNavigationState != LoginNavigationState.Done)
            {
                await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                return;
            }

            var json = Serializer.Serialize(creds);
            LoadUserAccountInfo(new() { json });

            await InitialAuthorization();
        }

        public async void LoadUserAccountInfo(List<string> data)
        {
            foreach (var accountData in data)
            {
                var credentials = Serializer.Deserialize<JooxCreds>(accountData);

                var newModel = new JooxModel();
                await newModel.Initialize(credentials);

                var accInfo = new AccountInfo()
                {
                    Creds = Serializer.Serialize(credentials),
                    Name = newModel.Email
                };

                Accounts.Add(newModel, accInfo);
            }
        }

        public override async Task<bool> Log_Out(bool forceUpdate = false)
        {
            SaveLoadCreds.DeleteServiceData();
            Accounts.Remove(Model);
            await Model.Logout().ConfigureAwait(false);
            RegState = RegistrationState.Unlogged;
            LogOutRequired = true;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in Tabs)
                    item.MediaItems.Clear();

                NavigateToMain();
            });

            return true;
        }

		#endregion AuthMethods

		#region TransferMethods

        public override async Task Transfer_SaveInTo(params object[] items)
        {
            MainViewModel.ResultVM.InitializeWhenServiceTransferring(this);
            IsSending = true;

            if (!Model.IsAuthenticated())
            {
                if (!SaveLoadCreds.IsCredsExists(out Dictionary<string, List<string>> data) || !await IsServiceDataExecuted(data))
                {
                    await Dispatcher.UIThread.InvokeAsync(NavigateToBrowserLoginPage);
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(NavigateToContent);
                }
            }
            else
            {
                WaitAuthentication.Set();
                IsSending = false;
            }

            await Transfer_DoWork(items[0]);
        }

        private new async Task Transfer_Send(Dictionary<MusConvPlayList, List<MusConvTrackSearchResult>> result, int index,
            IProgress<ReportCount> progressReport, CancellationToken token)
        {
            MainViewModel.ResultVM.SetPlaylistSearchItem(result);
            var indexor = 0;

            try
            {
                foreach (var resultKey in result.Keys)
                {
                    var creationRequestModel = new MusConvPlaylistCreationRequestModel(resultKey);
                    var createdPlaylist = await Model.CreatePlaylist(creationRequestModel).ConfigureAwait(false);
                    MainViewModel.ResultVM.MediaItemIds.Add(createdPlaylist.Id);

                    var resultLists = result[resultKey].Where(t => t?.ResultItems?.Count > 0)
                        .Select(x => x.ResultItems?.FirstOrDefault()).ToList()
                        .SplitList();

                    foreach (var item in resultLists)
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            await Model.AddTracksToPlaylist(createdPlaylist, item).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            MusConvLogger.LogFiles(e);
                        }

                        var message = $"Adding \"{result[resultKey][indexor].OriginalSearchItem.Title}\" to playlist \"{resultKey}\"";
                        await Dispatcher.UIThread.InvokeAsync(() =>
                            progressReport.Report(new ReportCount(item.Count, message, ReportType.Sending)));

                        indexor++;
                    }

                    indexor = 0;
                }
            }
            catch (Exception ex)
            {
                MusConvLogger.LogFiles(ex);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
                progressReport.Report(GetPlaylistsReportCount(result)));
        }

		#endregion TransferMethods
	}
}