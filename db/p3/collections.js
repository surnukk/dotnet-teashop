const database = db.getSiblingDB("EastLeaf_ReviewsDB");

["reviews", "tea_discussions", "tea_clubs"].forEach(name => {
  if (!database.getCollectionNames().includes(name)) {
    database.createCollection(name);
    print("created: " + name);
  } else {
    print("exists:  " + name);
  }
});

/* Структура документів (гнучка схема):

reviews: {
  _id, productId (long, = Catalog.Product.Id), productName,
  author: { userId (string), displayName },
  rating (1..5), text,
  comments: [ { author: {userId, displayName}, text, createdAt } ],   // вкладений масив
  discussionId?, teaware?: { material, capacityMl }, pros?: [], cons?: [],
  version (int, оптимістична конкурентність), createdAt, updatedAt?
}
tea_discussions: {
  _id, title, category, createdBy: {userId, displayName}, productIds: [long], tags: [],
  messageCount, lastMessageAt, pinnedMessages: [ {author, text, createdAt} ], createdAt
}
tea_clubs: {
  _id, name, city, description?, locations: [ {title, address, geo: {type:"Point", coordinates:[lng,lat]}} ],
  meetings?: [ {date, theme, seats} ], membersCount?, contacts: { telegram?, instagram?, email? }
}
*/

// ===== Індекси (для tea_discussions і tea_clubs; індекси reviews створює застосунок) =====
database.tea_clubs.createIndex({ city: 1 });
database.tea_clubs.createIndex({ "locations.geo": "2dsphere" });
database.tea_discussions.createIndex({ productIds: 1, lastMessageAt: -1 });
database.tea_discussions.createIndex({ title: "text", tags: "text" });

// ===== Aggregation 1: середній рейтинг і кількість коментарів по товару =====
print("--- avg rating per product ---");
database.reviews.aggregate([
  { $group: {
      _id: "$productId",
      productName: { $first: "$productName" },
      avgRating: { $avg: "$rating" },
      reviews: { $sum: 1 },
      comments: { $sum: { $size: { $ifNull: ["$comments", []] } } } } },
  { $sort: { avgRating: -1 } }
]).forEach(printjson);

// ===== Aggregation 2: відгуки з обговоренням ($lookup по reference discussionId) =====
print("--- reviews with discussion ---");
database.reviews.aggregate([
  { $match: { discussionId: { $exists: true } } },
  { $lookup: { from: "tea_discussions", localField: "discussionId",
               foreignField: "_id", as: "discussion" } },
  { $unwind: "$discussion" },
  { $project: { productName: 1, rating: 1,
                "discussion.title": 1, "discussion.messageCount": 1 } }
]).forEach(printjson);

// ===== Геопошук: клуби в радіусі 50 км від Чернівців =====
print("--- clubs near Chernivtsi ---");
database.tea_clubs.find({
  "locations.geo": { $near: { $geometry: { type: "Point", coordinates: [25.93, 48.29] },
                              $maxDistance: 50000 } }
}).forEach(printjson);