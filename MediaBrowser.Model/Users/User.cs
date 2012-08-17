﻿using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Users
{
    public class User : BaseEntity
    {
        public string MaxParentalRating { get; set; }

        private Dictionary<Guid, UserItemData> _ItemData = new Dictionary<Guid, UserItemData>();
        public Dictionary<Guid, UserItemData> ItemData { get { return _ItemData; } set { _ItemData = value; } }

        public int RecentItemDays { get; set; }

        public User()
        {
            RecentItemDays = 14;
        }

        /// <summary>
        /// Gets user data for an item, if there is any
        /// </summary>
        public UserItemData GetItemData(Guid itemId)
        {
            if (ItemData.ContainsKey(itemId))
            {
                return ItemData[itemId];
            }

            return null;
        }
    }
}
