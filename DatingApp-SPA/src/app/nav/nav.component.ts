import { Component, OnInit } from '@angular/core';
import { AuthService } from '../_services/auth.service';
import { AlertifyService } from '../_services/alertify.service';
import { Router } from '@angular/router';
import { AlertModule } from 'ngx-bootstrap/alert';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})

export class NavComponent implements OnInit {
  model: any = {};
  photoUrl: string;
  isFirstTime = true;

  constructor(public authService: AuthService, private alertify: AlertifyService, private router: Router) { }

  ngOnInit() {
    this.authService.currentPhotoUrl.subscribe(photoUrl => this.photoUrl = photoUrl);

    if (localStorage.getItem('seenNotice') != null)
    {
      this.isFirstTime = false;
      localStorage.setItem('seenNoticed', 'seen');
    }
  }

  login(){
    this.authService.login(this.model).subscribe(next => {
      this.alertify.success('You have logged in successfully.');
    }, error => {
      this.alertify.error(error);
    }, () => {
      this.router.navigate(['/members']);
    });
  }

  loggedIn(){
    return this.authService.loggedIn();
  }

  logout(){
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.authService.decodedToken = null;
    this.authService.currentUser = null;
    this.alertify.message('You have been logged out.');
    this.router.navigate(['/home']);
  }
}
