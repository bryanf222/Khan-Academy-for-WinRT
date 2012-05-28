﻿using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using KhanViewer.Models;

namespace KhanViewer
{
    [DataContract]
    public class PlaylistItem : Item
    {
        private bool loaded = false;

        public PlaylistItem()
        {
            this.Videos = new ObservableCollection<VideoItem>();
        }

        /// <summary>List of videos in this category</summary>
        [DataMember]
        public ObservableCollection<VideoItem> Videos { get; set; }

        [DataMember]
        public string Slug { get; set; }

        /// <summary>This is the top level group</summary>
        /// <remarks>Parsed from the first element in the slug</remarks>
        public string GroupKey
        {
            get
            {
                // make sure not empty
                if (string.IsNullOrWhiteSpace(this.Slug)) return string.Empty;

                string value;

                if (!this.Slug.Contains("/"))
                {
                    value = this.Slug;
                }
                else
                {
                    value = this.Slug.Substring(0, this.Slug.IndexOf("/"));
                }

                value = value.Replace("-", " ");

                return value;
            }
        }

        public void LoadVideos()
        {
            if (!loaded)
            {
                // first load what I know (ie. from disk)
                LocalStorage.GetVideos(this.Name, vids =>
                    {
                        UIThread.Invoke(() => { foreach (var vid in vids) Videos.Add(vid); });

                        loaded = true;
                    });

                // now kick off the server to the query
                Clouds.GetVideosFromServer(this.Videos, this.Name);
            }
            else if (this.Videos.Count == 0)
            {
                // if we've already loaded, but don't have any results, then need to try again
                Clouds.GetVideosFromServer(this.Videos, this.Name);
            }
        }

        public static void Initialize(ObservableCollection<GroupItem> groups, ObservableCollection<PlaylistItem> items)
        {
            // first load what I know
            LocalStorage.GetPlaylists(playlists =>
                {
                    var grouped = GroupItem.CreateGroups(playlists);

                    UIThread.Invoke(() => 
                    {
                        groups.Clear();
                        items.Clear();
                        foreach (var group in grouped) groups.Add(group);
                        foreach (var list in playlists) items.Add(list); 
                    });

                    // then start to query the server
                    Clouds.LoadCategoriesFromServer(groups, items);
                });
        }
    }
}