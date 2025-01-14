﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kroeg.ActivityStreams;
using Kroeg.EntityStore.Models;
using Kroeg.EntityStore.Store;
using System.Data;
using Dapper;
using System.Data.Common;
using Kroeg.Services;
using Kroeg.EntityStore.Services;

namespace Kroeg.ActivityPub.ClientToServer
{
  public class CreateActorHandler : BaseHandler
  {
    private readonly CollectionTools _collection;
    private readonly DbConnection _connection;
    public string UserOverride { get; set; }

    public CreateActorHandler(
        IEntityStore entityStore,
        APEntity mainObject,
        APEntity actor,
        APEntity targetBox,
        ClaimsPrincipal user,
        CollectionTools collection,
        DbConnection connection) : base(entityStore, mainObject, actor, targetBox, user)
    {
      _collection = collection;
      _connection = connection;
    }

    private async Task<APEntity> AddCollection(ASObject entity, string obj, string parent, string store = null)
    {
      var collection = await _collection.NewCollection(EntityStore, null, "_" + obj, parent);
      var data = collection.Data;
      data.Replace("attributedTo", ASTerm.MakeId(parent));
      collection.Data = data;

      await EntityStore.StoreEntity(collection);

      entity.Replace(store ?? obj, ASTerm.MakeId(collection.Id));
      return collection;
    }

    private void _merge(List<ASTerm> to, List<ASTerm> from)
    {
      var str = new HashSet<string>(to.Select(a => a.Id).Concat(from.Select(a => a.Id)));

      to.Clear();
      to.AddRange(str.Select(a => ASTerm.MakeId(a)));
    }

    public override async Task<bool> Handle()
    {
      if (MainObject.Type != "https://www.w3.org/ns/activitystreams#Create")
      {
        return true;
      }

      var activityData = MainObject.Data;
      var objectEntity = await EntityStore.GetEntity(activityData["object"].First().Id, false);

      if (!EntityData.IsActor(objectEntity.Data))
      {
        return true;
      }

      var objectData = objectEntity.Data;
      var id = objectEntity.Id;

      await AddCollection(objectData, "inbox", id);
      await AddCollection(objectData, "outbox", id);
      await AddCollection(objectData, "following", id);
      await AddCollection(objectData, "followers", id);
      await AddCollection(objectData, "liked", id);

      var blocks = await AddCollection(objectData, "blocks", id);
      var blocked = await _collection.NewCollection(EntityStore, null, "blocked", blocks.Id);

      var blocksData = blocks.Data;
      blocksData["blocked"].Add(ASTerm.MakeId(blocked.Id));
      blocks.Data = blocksData;

      if (!objectData["manuallyApprovesFollowers"].Any())
      {
        objectData.Replace("manuallyApprovesFollowers", ASTerm.MakePrimitive(false));
      }

      objectEntity.Data = objectData;

      await EntityStore.StoreEntity(blocked);
      await EntityStore.StoreEntity(blocks);
      await EntityStore.StoreEntity(objectEntity);

      var userId = UserOverride ?? User.FindFirst(ClaimTypes.NameIdentifier).Value;

      if (!activityData["actor"].Any())
      {
        activityData["actor"].Add(ASTerm.MakeId(objectEntity.Id));
      }

      MainObject.Data = activityData;
      await EntityStore.StoreEntity(MainObject);

      await _connection.ExecuteAsync("insert into \"UserActorPermissions\" (\"UserId\", \"ActorId\", \"IsAdmin\") values (@UserId, @ActorId, TRUE)", new { UserId = userId, ActorId = objectEntity.DbId });

      return true;
    }
  }
}
