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