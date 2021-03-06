﻿using System.Collections.Generic;
using System.Linq;
using SpecialOffers.Domains;

namespace SpecialOffers.Stores
{
    public class SpecialOfferStore : ISpecialOfferStore
    {
        private static readonly IDictionary<int, SpecialOffer> _database = new Dictionary<int, SpecialOffer>();
        private readonly IEventStore _eventStore;

        public SpecialOfferStore(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public SpecialOffer Get(int offerId)
        {
            _database.TryGetValue(offerId, out SpecialOffer offer);
            return offer;
        }

        public void Add(SpecialOffer specialOffer)
        {
            var offerId = _database.Keys.Any() ? _database.Keys.Max() + 1 : 1;
            specialOffer.Id = offerId;
            _database.Add(offerId, specialOffer);
            _eventStore.Raise("NewSpecialOffer", specialOffer);
        }

        public void Update(SpecialOffer specialOffer)
        {
            _database[specialOffer.Id] = specialOffer;
            _eventStore.Raise("UpdatedSpecialOffer", specialOffer);
        }

        public void Save()
        {
            // Nothing needed. Saving would be needed with a real DB
        }
    }
}