namespace Dy.MedicalRecognition.Tests;

/// <summary>外部用户服务的最小共享替身：按用户标识返回预置档案。</summary>
internal sealed class StubUserAppService : IUserAppService
{
  public Dictionary<Guid, UserDto> UserById { get; } = [];
  public int GetUserByIdCallCount { get; private set; }
  public Task<UserDto> GetUserByIdAsync(GetUserByIdRequest request)
  {
    GetUserByIdCallCount++;
    return Task.FromResult(UserById.TryGetValue(request.Id, out UserDto? user) ? user : null!);
  }
}
