const database = db.getSiblingDB("EastLeaf_ReviewsDB");

database.reviews.deleteMany({});
database.tea_discussions.deleteMany({});
database.tea_clubs.deleteMany({});

database.reviews.insertMany([
  {
    _id: ObjectId("66f0c2000000000000000001"),
    productId: 1,
    productName: "Да Хун Пао",
    author: { userId: 1, displayName: "Олена" },
    rating: 5,
    text: "Глибокий мінеральний смак, витримує вісім проливів.",
    comments: [
      {
        author: { userId: 2, displayName: "Андрій" },
        text: "Згоден, брав цей самий збір.",
        createdAt: ISODate("2026-09-13T09:00:00Z")
      }
    ],
    discussionId: ObjectId("66f0c3000000000000000001"),
    createdAt: ISODate("2026-09-12T10:30:00Z")
  },
  {
    _id: ObjectId("66f0c2000000000000000002"),
    productId: 3,
    productName: "Шу Пуер",
    author: { userId: 2, displayName: "Андрій" },
    rating: 4,
    text: "М'який, землистий, добре для вечора.",
    createdAt: ISODate("2026-09-15T19:10:00Z")
  },
  {
    _id: ObjectId("66f0c2000000000000000003"),
    productId: 2,
    productName: "Ісинський чайник 150 мл",
    author: { userId: 3, displayName: "Марко" },
    rating: 5,
    text: "Гарно тримає тепло, носик не капає.",
    teaware: { material: "Ісинська глина", capacityMl: 150 },
    pros: ["тепло", "носик"],
    cons: ["потребує обережного миття"],
    createdAt: ISODate("2026-09-18T12:45:00Z")
  }
]);

database.tea_discussions.insertMany([
  {
    _id: ObjectId("66f0c3000000000000000001"),
    title: "Скільки проливів витримує Да Хун Пао?",
    category: "brewing",
    createdBy: { userId: 1, displayName: "Олена" },
    productIds: [1],
    tags: ["улун", "заварювання"],
    messageCount: 2,
    lastMessageAt: ISODate("2026-09-14T18:20:00Z"),
    pinnedMessages: [
      {
        author: { userId: 1, displayName: "Олена" },
        text: "Починайте з 95 градусів і коротких проливів.",
        createdAt: ISODate("2026-09-12T10:35:00Z")
      }
    ],
    createdAt: ISODate("2026-09-12T10:35:00Z")
  }
]);

database.tea_clubs.insertMany([
  {
    _id: ObjectId("66f0c4000000000000000001"),
    name: "Чернівецький чайний клуб",
    city: "Чернівці",
    description: "Щотижневі дегустації улунів і пуерів.",
    locations: [
      {
        title: "Чайна кімната",
        address: "вул. Приклад, 1",
        geo: { type: "Point", coordinates: [25.94, 48.29] }
      }
    ],
    meetings: [
      { date: ISODate("2026-10-18T16:00:00Z"), theme: "Дегустація Да Хун Пао", seats: 12 }
    ],
    contacts: { telegram: "@example_club" }
  },
  {
    _id: ObjectId("66f0c4000000000000000002"),
    name: "Київський клуб гайвані",
    city: "Київ",
    locations: [
      {
        title: "Студія на Подолі",
        address: "вул. Приклад, 5",
        geo: { type: "Point", coordinates: [30.52, 50.46] }
      }
    ],
    membersCount: 40,
    contacts: { instagram: "example_gaiwan_kyiv", email: "club@example.com" }
  }
]);

print("reviews: " + database.reviews.countDocuments() +
      ", tea_discussions: " + database.tea_discussions.countDocuments() +
      ", tea_clubs: " + database.tea_clubs.countDocuments());
