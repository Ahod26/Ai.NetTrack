export class UserInfo {
  constructor(data) {
    this.fullName = data.fullName || "";
    this.email = data.email || "";
    this.roles = data.roles || [];
    this.isSubscribedToNewsletter = data.isSubscribedToNewsletter ?? false;
  }
}

export class LoginResponse {
  constructor(data) {
    this.success = data.success || false;
    this.message = data.message || "";

    // Backend returns userInfo with nested apiUserDto
    if (data.userInfo && data.userInfo.apiUserDto) {
      this.user = new UserInfo({
        fullName: data.userInfo.apiUserDto.fullName,
        email: data.userInfo.apiUserDto.email,
        roles: data.userInfo.roles,
        isSubscribedToNewsletter:
          data.userInfo.apiUserDto.isSubscribedToNewsletter,
      });
    } else {
      this.user = data.user ? new UserInfo(data.user) : null;
    }
  }
}

export class RegisterResponse {
  constructor(data) {
    this.success = data.success || false;
    this.message = data.message || "";
    this.errors = data.errors || [];
  }
}
