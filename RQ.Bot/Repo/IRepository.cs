using System.Collections;
using Microsoft.Extensions.Primitives;
using RQ.DTO;

namespace Bot.Repo;

public interface IRepository
{
    bool IsKnownToken(string value);
    bool TryGetUserById(long userId, out UserData user);
    RefRequest[] GetAllRequest();
    RefRequest[] GetAllRequestFromUser(long userId);
    RefRequest GetRequest(Guid requestId);
    void UpdateRefRequest(RefRequest request);
    bool TryGetActiveUserRequest(long userId, out RefRequest refRequest);
    UserData[] GetAdminUsers();
    UserData[] GetAllUsers();
    void UpsertUser(UserData rfUser);
}