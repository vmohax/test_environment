using MusConv.ViewModels.Enum;
using MusConv.ViewModels.Helper;
using MusConv.ViewModels.Models.MusicService;
using MusConv.ViewModels.ViewModels.Base;
using MusConv.ViewModels.ViewModels.Base.Tabs;
using MusConv.ViewModels.ViewModels.Base.TaskItems;
using System.Collections.Generic;
using System.IO;
using MusConv.ViewModels.ViewModels.Base.Commands;
using MusConv.ViewModels.ViewModels.Base.Commands.Import;
using MusConv.ViewModels.ViewModels.Base.Commands.View;
using MusConv.Shared.SharedAbstractions.Enums;

namespace MusConv.ViewModels.ViewModels.SectionViewModels
{
    public class AudirvanaViewModel : M3UViewModelBase
	{
		#region Constructors

		public AudirvanaViewModel(MainViewModelBase m) : base(m)
        {
			Title = "Audirvana";
			SourceType = DataSource.Audirvana;
			LogoKey = LogoStyleKey.AudirvanaLogo;
			SideLogoKey = LeftSideBarLogoKey.AudirvanaSideLogo;
			//can`t be destination for autosync, need to implement method AddTracksToPlaylist for all file services
			IsSuitableForAutoSync = false;
			Model = new AudirvanaModel();

			#region Commands

			var commandPlaylistsTab = new List<Command_TaskItem> (PlaylistsTabCommandsBase)
			{
				new DeleteCommand(Command_MultiDelete, CommandTaskType.DropDownMenu),
				new DeleteCommand(Command_MultiDelete, CommandTaskType.CommandBar),
				new ImportM3UFileCommand(ImportFileCommand, CommandTaskType.CommandBar),
			};

			var commandTracks = new List<Command_TaskItem> (TracksCommandsBase)
			{
				new ViewOnYouTubeCommand(CommandTrack_Open),
				new ViewArtistCommand(CommandTrack_OpenArtist),
            };

			#endregion Commands

			var playlistTransfer = new List<TaskBase_TaskItem>
			{
				new PlaylistTransfer_TaskItem(SourceType, LwTabStyleKey.ItemStylePlaylist, null, null, m)
			};

			var playlistsTab = new PlaylistTabViewModelBase(m, LwTabIconKey.PlaylistIcon, 
				playlistTransfer, commandPlaylistsTab,
				new Initial_TaskItem("Reload", Initial_Update_Playlists), commandTracks);

			Tabs.Add(playlistsTab);
		}

		#endregion Constructors
	}
}