export class UserInfo {
  constructor(data) {
    this.userName = data.userName || '';
    this.email = data.email || '';
    this.roles = data.roles || [];
  }
}

export class LoginResponse {
  constructor(data) {
    this.success = data.success || false;
    this.message = data.message || '';
    this.user = data.user ? new UserInfo(data.user) : null;
  }
}

export class RegisterResponse{
  constructor(data){
    this.success = data.success || false;
    this.message = data.message || '';
    this.errors = data.errors || [];
  }
}