﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
        public static BaseItem GetItemById(string id)
        {
            Guid guid = string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);

            return Kernel.Instance.GetItemById(guid);
        }

        /// <summary>
        /// Takes a BaseItem and returns the actual object that will be serialized by the api
        /// </summary>
        public static BaseItemContainer<BaseItem> GetSerializationObject(BaseItem item, bool includeChildren, Guid userId)
        {
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            BaseItemContainer<BaseItem> wrapper = new BaseItemContainer<BaseItem>()
            {
                Item = item,
                UserItemData = user.GetItemData(item.Id),
                Type = item.GetType().Name,
                IsFolder = (item is Folder)
            };

            if (string.IsNullOrEmpty(item.LogoImagePath))
            {
                wrapper.ParentLogoItemId = GetParentLogoItemId(item);
            }

            if (item.BackdropImagePaths == null || !item.BackdropImagePaths.Any())
            {
                int backdropCount;
                wrapper.ParentBackdropItemId = GetParentBackdropItemId(item, out backdropCount);
                wrapper.ParentBackdropCount = backdropCount;
            }

            if (item.Parent != null)
            {
                wrapper.ParentId = item.Parent.Id;
            }

            if (includeChildren)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    wrapper.Children = folder.GetParentalAllowedChildren(user).Select(c => GetSerializationObject(c, false, userId));
                }

                // Attach People by transforming them into BaseItemPerson (DTO)
                if (item.People != null)
                {
                    wrapper.People = item.People.Select(p =>
                    {
                        BaseItemPerson baseItemPerson = new BaseItemPerson();

                        baseItemPerson.PersonInfo = p;

                        Person ibnObject = Kernel.Instance.ItemController.GetPerson(p.Name);

                        if (ibnObject != null)
                        {
                            baseItemPerson.PrimaryImagePath = ibnObject.PrimaryImagePath;
                        }

                        return baseItemPerson;
                    });
                }
            }

            // Attach Studios by transforming them into BaseItemStudio (DTO)
            if (item.Studios != null)
            {
                wrapper.Studios = item.Studios.Select(s =>
                {
                    BaseItemStudio baseItemStudio = new BaseItemStudio();

                    baseItemStudio.Name = s;

                    Studio ibnObject = Kernel.Instance.ItemController.GetStudio(s);

                    if (ibnObject != null)
                    {
                        baseItemStudio.PrimaryImagePath = ibnObject.PrimaryImagePath;
                    }

                    return baseItemStudio;
                });
            }

            return wrapper;
        }

        private static Guid? GetParentBackdropItemId(BaseItem item, out int backdropCount)
        {
            backdropCount = 0;

            var parent = item.Parent;

            while (parent != null)
            {
                if (parent.BackdropImagePaths != null && parent.BackdropImagePaths.Any())
                {
                    backdropCount = parent.BackdropImagePaths.Count();
                    return parent.Id;
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static Guid? GetParentLogoItemId(BaseItem item)
        {
            var parent = item.Parent;

            while (parent != null)
            {
                if (!string.IsNullOrEmpty(parent.LogoImagePath))
                {
                    return parent.Id;
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static string _FFMpegDirectory = null;
        /// <summary>
        /// Gets the folder path to ffmpeg
        /// </summary>
        public static string FFMpegDirectory
        {
            get
            {
                if (_FFMpegDirectory == null)
                {
                    _FFMpegDirectory = System.IO.Path.Combine(ApplicationPaths.ProgramDataPath, "ffmpeg");

                    if (!Directory.Exists(_FFMpegDirectory))
                    {
                        Directory.CreateDirectory(_FFMpegDirectory);
                    }
                }

                return _FFMpegDirectory;
            }
        }

        private static string _FFMpegPath = null;
        /// <summary>
        /// Gets the path to ffmpeg.exe
        /// </summary>
        public static string FFMpegPath
        {
            get
            {
                if (_FFMpegPath == null)
                {
                    string filename = "ffmpeg.exe";

                    _FFMpegPath = Path.Combine(FFMpegDirectory, filename);

                    // Always re-extract the first time to handle new versions
                    if (File.Exists(_FFMpegPath))
                    {
                        File.Delete(_FFMpegPath);
                    }

                    // Extract ffprobe
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.Api.FFMpeg." + filename))
                    {
                        using (FileStream fileStream = new FileStream(_FFMpegPath, FileMode.Create))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }

                return _FFMpegPath;
            }
        }
    }
}
