const database = db.getSiblingDB("EastLeaf_ReviewsDB");

function upsert(collection, doc) {
  collection.replaceOne({ _id: doc._id }, doc, { upsert: true });
}

// ---- reviews (productId = Id from EastLeaf_CatalogDB; userId is a string; version for optimistic concurrency) ----
upsert(database.reviews, {
  _id: ObjectId("66f0c2000000000000000001"),
  productId: NumberLong("4"), productName: "Da Hong Pao Oolong",
  author: { userId: "1", displayName: "Olena" },
  rating: 5,
  text: "Deep mineral taste, holds up to six infusions.",
  comments: [{
    _id: ObjectId("66f0c5000000000000000001"),
    author: { userId: "2", displayName: "Ivan" },
    text: "Agreed, a really good oolong.",
    createdAt: ISODate("2026-10-06T09:00:00Z")
  }],
  discussionId: ObjectId("66f0c3000000000000000001"),
  version: 0,
  createdAt: ISODate("2026-10-05T10:30:00Z")
});

upsert(database.reviews, {
  _id: ObjectId("66f0c2000000000000000002"),
  productId: NumberLong("3"), productName: "Shu Pu-erh Aged 2018",
  author: { userId: "2", displayName: "Ivan" },
  rating: 4,
  text: "Smooth and earthy, great for the evening.",
  comments: [],
  version: 0,
  createdAt: ISODate("2026-10-08T19:10:00Z")
});

upsert(database.reviews, {
  _id: ObjectId("66f0c2000000000000000003"),
  productId: NumberLong("5"), productName: "Ceramic Gaiwan Set",
  author: { userId: "3", displayName: "Maria" },
  rating: 5,
  text: "Comfortable gaiwan, opens up the aroma nicely.",
  comments: [],
  teaware: { material: "Ceramic", capacityMl: 150 },
  pros: ["aroma", "comfortable rim"],
  cons: ["gets hot"],
  version: 0,
  createdAt: ISODate("2026-10-09T12:45:00Z")
});

// ---- tea_discussions ----
upsert(database.tea_discussions, {
  _id: ObjectId("66f0c3000000000000000001"),
  title: "How many infusions does Da Hong Pao hold?",
  category: "brewing",
  createdBy: { userId: "1", displayName: "Olena" },
  productIds: [NumberLong("4")],
  tags: ["oolong", "brewing"],
  messageCount: 2,
  lastMessageAt: ISODate("2026-10-07T18:20:00Z"),
  pinnedMessages: [{
    author: { userId: "1", displayName: "Olena" },
    text: "Start at 90 degrees with short infusions.",
    createdAt: ISODate("2026-10-05T10:35:00Z")
  }],
  createdAt: ISODate("2026-10-05T10:35:00Z")
});

// ---- tea_clubs ----
upsert(database.tea_clubs, {
  _id: ObjectId("66f0c4000000000000000001"),
  name: "Chernivtsi Tea Club", city: "Chernivtsi",
  description: "Weekly tastings of oolongs and pu-erhs.",
  locations: [{ title: "Tea Room", address: "1 Example St.",
                geo: { type: "Point", coordinates: [25.94, 48.29] } }],
  meetings: [{ date: ISODate("2026-10-18T16:00:00Z"), theme: "Da Hong Pao tasting", seats: 12 }],
  contacts: { telegram: "@example_club" }
});

upsert(database.tea_clubs, {
  _id: ObjectId("66f0c4000000000000000002"),
  name: "Kyiv Gaiwan Club", city: "Kyiv",
  locations: [{ title: "Podil Studio", address: "5 Example St.",
                geo: { type: "Point", coordinates: [30.52, 50.46] } }],
  membersCount: 40,
  contacts: { instagram: "example_gaiwan_kyiv", email: "club@example.com" }
});

print("reviews: " + database.reviews.countDocuments() +
      ", tea_discussions: " + database.tea_discussions.countDocuments() +
      ", tea_clubs: " + database.tea_clubs.countDocuments());