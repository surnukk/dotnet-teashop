const database = db.getSiblingDB("EastLeaf_ReviewsDB");

function upsert(collection, doc) {
  collection.replaceOne({ _id: doc._id }, doc, { upsert: true });
}

upsert(database.reviews, {
  _id: ObjectId("66f0c2000000000000000001"),
  productId: 4, productName: "Da Hong Pao Oolong",
  author: { userId: 1, displayName: "Олена" },
  rating: 5,
  text: "Глибокий мінеральний смак, витримує шість проливів.",
  comments: [{
    author: { userId: 2, displayName: "Іван" },
    text: "Згоден, дуже гарний улун.",
    createdAt: ISODate("2026-10-06T09:00:00Z")
  }],
  discussionId: ObjectId("66f0c3000000000000000001"),
  createdAt: ISODate("2026-10-05T10:30:00Z")
});

upsert(database.reviews, {
  _id: ObjectId("66f0c2000000000000000002"),
  productId: 3, productName: "Shu Pu-erh Aged 2018",
  author: { userId: 2, displayName: "Іван" },
  rating: 4,
  text: "М'який, землистий, добре для вечора.",
  createdAt: ISODate("2026-10-08T19:10:00Z")
});

upsert(database.reviews, {
  _id: ObjectId("66f0c2000000000000000003"),
  productId: 5, productName: "Ceramic Gaiwan Set",
  author: { userId: 3, displayName: "Марія" },
  rating: 5,
  text: "Зручна гайвань, добре розкриває аромат.",
  teaware: { material: "Кераміка", capacityMl: 150 },
  pros: ["аромат", "зручний борт"],
  cons: ["нагрівається"],
  createdAt: ISODate("2026-10-09T12:45:00Z")
});

upsert(database.tea_discussions, {
  _id: ObjectId("66f0c3000000000000000001"),
  title: "Скільки проливів витримує Da Hong Pao?",
  category: "brewing",
  createdBy: { userId: 1, displayName: "Олена" },
  productIds: [4],
  tags: ["улун", "заварювання"],
  messageCount: 2,
  lastMessageAt: ISODate("2026-10-07T18:20:00Z"),
  pinnedMessages: [{
    author: { userId: 1, displayName: "Олена" },
    text: "Починайте з 90 градусів і коротких проливів.",
    createdAt: ISODate("2026-10-05T10:35:00Z")
  }],
  createdAt: ISODate("2026-10-05T10:35:00Z")
});

upsert(database.tea_clubs, {
  _id: ObjectId("66f0c4000000000000000001"),
  name: "Чернівецький чайний клуб", city: "Чернівці",
  description: "Щотижневі дегустації улунів і пуерів.",
  locations: [{ title: "Чайна кімната", address: "вул. Приклад, 1",
                geo: { type: "Point", coordinates: [25.94, 48.29] } }],
  meetings: [{ date: ISODate("2026-10-18T16:00:00Z"), theme: "Дегустація Da Hong Pao", seats: 12 }],
  contacts: { telegram: "@example_club" }
});

upsert(database.tea_clubs, {
  _id: ObjectId("66f0c4000000000000000002"),
  name: "Київський клуб гайвані", city: "Київ",
  locations: [{ title: "Студія на Подолі", address: "вул. Приклад, 5",
                geo: { type: "Point", coordinates: [30.52, 50.46] } }],
  membersCount: 40,
  contacts: { instagram: "example_gaiwan_kyiv", email: "club@example.com" }
});

print("reviews: " + database.reviews.countDocuments() +
      ", tea_discussions: " + database.tea_discussions.countDocuments() +
      ", tea_clubs: " + database.tea_clubs.countDocuments());