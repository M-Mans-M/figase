"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/newsHub").build();

connection.on("NewPost", function (postId, personId, created, content) {
    console.log('New post fire: ', postId, personId, created, content);

    //<div class="row post" title="ID: @post.Id">
    //    <span class="post-created">@post.Created</span> <span title="Источник" class="post-person">[@post.PersonId]</span> @post.Content
    //</div>

    var div = document.createElement("div");
    div.classList.add('row');
    div.classList.add('post');
    div.title = 'ID: ' + postId;

    div.innerHTML = '<span class="post-created">' + created + '</span> <span title="Источник" class="post-person">[' + personId + ']</span> ' + content;

    if (document.getElementById("fake-news")) document.getElementById("fake-news").remove();
    document.getElementById("news-container").prepend(div);
});

connection.start().then(function () {

}).catch(function (err) {
    return console.error(err.toString());
});