using Dy.Base.Application.Contracts.UserAggregate;
using Dy.Core.Abstractions.Http;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 外部用户服务的最小共享替身：按用户标识返回预置的当前登录用户档案，其余成员调用即失败。
/// </summary>
/// <remarks>
/// 默认不预置任何档案，因此"读不到用户档案"这一拒绝路径可以被用例直接构造，
/// 阶段 2 与阶段 3 既有的"可信组织或医院缺失即拒绝"用例语义保持不变；
/// 需要用户档案补齐的用例按用户标识预置档案，从而把令牌缺失层与用户档案补齐分开构造。
/// 未实现的成员一律抛出，避免用例在未覆盖的路径上静默走过。
/// </remarks>
internal sealed class StubUserAppService : IUserAppService
{
  /// <summary>按用户标识预置的当前登录用户档案；未预置的标识读取返回 null。</summary>
  public Dictionary<Guid, UserDto> UserById { get; } = [];

  /// <summary>按用户标识读取当前登录用户档案的调用次数。</summary>
  public int GetUserByIdCallCount { get; private set; }

  /// <inheritdoc/>
  public Task<UserDto> GetUserByIdAsync(GetUserByIdRequest getUserByIdRequest)
  {
    GetUserByIdCallCount++;
    return Task.FromResult(UserById.TryGetValue(getUserByIdRequest.Id, out UserDto? user) ? user : null!);
  }

  /// <inheritdoc/>
  public Task<string> GetIpAsync() => Unsupported<string>();

  /// <inheritdoc/>
  public Task<TokenInfo> LoginAsync(LoginRequest loginRequest) => Unsupported<TokenInfo>();

  /// <inheritdoc/>
  public Task<TokenInfo> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest) => Unsupported<TokenInfo>();

  /// <inheritdoc/>
  public Task<Guid> CreateLoginRecordAsync(CreateLoginRecordRequest createLoginRecordRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> EnableLoginRecordAsync(EnableLoginRecordRequest enableLoginRecordRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableLoginRecordAsync(DisableLoginRecordRequest disableLoginRecordRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<IEnumerable<LoginRecordDto>> QueryUserLoginRecordAsync(QueryUserLoginRecordRequest queryUserLoginRecordRequest) => Unsupported<IEnumerable<LoginRecordDto>>();

  /// <inheritdoc/>
  public Task<LoginRecordDto> GetLoginRecordByIdAsync(GetLoginRecordByIdRequest getLoginRecordByIdRequest) => Unsupported<LoginRecordDto>();

  /// <inheritdoc/>
  public Task<int> DisableUserLoginTokenAsync(DisableUserLoginTokenRequest disableUserLoginTokenRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteLoginRecordAsync(DeleteLoginRecordRequest deleteLoginRecordRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<Guid> CreateUserAsync(CreateUserRequest createUserRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> UpdateUserAsync(UpdateUserRequest updateUserRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> UpdatePersonalInfoAsync(UpdatePersonalInfoRequest updatePersonalInfoRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DisableUserAsync(DisableUserRequest disableUserRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> LockUserAsync(LockUserRequest lockUserRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> ActiveUserAsync(ActiveUserRequest activeUserRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> ChangeAvatarAsync(ChangeAvatarRequest changeAvatarRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteUserAsync(DeleteUserRequest deleteUserRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<UserDto> GetUserByUserNameAsync(GetUserByUserNameRequest getUserByUserNameRequest) => Unsupported<UserDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<UserDto>> QueryOrgUserByTypeAsync(QueryOrgUserByTypeRequest queryOrgUserByTypeRequest) => Unsupported<IEnumerable<UserDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<UserDto>> QueryUserByOrgAsync(QueryUserByOrgRequest queryUserByOrgRequest) => Unsupported<IEnumerable<UserDto>>();

  /// <inheritdoc/>
  public Task<IEnumerable<UserDto>> QueryAllUserAsync() => Unsupported<IEnumerable<UserDto>>();

  /// <inheritdoc/>
  public Task<Guid> CreateUserDeptAsync(CreateUserDeptRequest createUserDeptRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> DeleteUserDeptAsync(DeleteUserDeptRequest deleteUserDeptRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<IEnumerable<UserDeptDto>> QueryUserDeptAsync(QueryUserDeptRequest queryUserDeptRequest) => Unsupported<IEnumerable<UserDeptDto>>();

  /// <inheritdoc/>
  public Task<UserDeptDto> GetUserDeptAsync(GetUserDeptRequest getUserDeptRequest) => Unsupported<UserDeptDto>();

  /// <inheritdoc/>
  public Task<UserDeptDto> GetUserDeptByIdAsync(GetUserDeptByIdRequest getUserDeptByIdRequest) => Unsupported<UserDeptDto>();

  /// <inheritdoc/>
  public Task<Guid> GrantSubApplicationAsync(GrantSubApplicationRequest grantSubApplicationRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> DeleteUserSubApplicationAsync(DeleteUserSubApplicationRequest deleteUserSubApplicationRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<int> DeleteUserSubApplicationByUserIdSubApplicationIdAsync(
    DeleteUserSubApplicationByUserIdSubApplicationIdRequest deleteUserSubApplicationByUserIdSubApplicationIdRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<UserSubApplicationDto> GetUserSubApplicationByUserIdSubApplicationIdAsync(
    GetUserSubApplicationByUserIdSubApplicationIdRequest getUserSubApplicationByUserIdSubApplicationIdRequest) => Unsupported<UserSubApplicationDto>();

  /// <inheritdoc/>
  public Task<IEnumerable<UserSubApplicationDto>> QueryUserSubApplicationByUserIdAsync(
    QueryUserSubApplicationByUserIdRequest queryUserSubApplicationByUserIdRequest) => Unsupported<IEnumerable<UserSubApplicationDto>>();

  /// <inheritdoc/>
  public Task<UserSubApplicationDto> GetUserSubApplicationByIdAsync(
    GetUserSubApplicationByIdRequest getUserSubApplicationByIdRequest) => Unsupported<UserSubApplicationDto>();

  /// <inheritdoc/>
  public Task<Guid> CreateUserWardAsync(CreateUserWardRequest createUserWardRequest) => Unsupported<Guid>();

  /// <inheritdoc/>
  public Task<int> DeleteUserWardAsync(DeleteUserWardRequest deleteUserWardRequest) => Unsupported<int>();

  /// <inheritdoc/>
  public Task<IEnumerable<UserWardDto>> QueryUserWardAsync(QueryUserWardRequest queryUserWardRequest) => Unsupported<IEnumerable<UserWardDto>>();

  /// <inheritdoc/>
  public Task<UserWardDto> GetUserWardAsync(GetUserWardRequest getUserWardRequest) => Unsupported<UserWardDto>();

  /// <inheritdoc/>
  public Task<UserWardDto> GetUserWardByIdAsync(GetUserWardByIdRequest getUserWardByIdRequest) => Unsupported<UserWardDto>();

  /// <summary>本替身未实现该外部用户服务成员。</summary>
  /// <typeparam name="TResult">成员返回类型。</typeparam>
  /// <returns>不会返回。</returns>
  /// <exception cref="NotSupportedException">该成员被调用时抛出。</exception>
  private static Task<TResult> Unsupported<TResult>() =>
    throw new NotSupportedException("共享替身只实现按用户标识读取当前登录用户档案。");
}
