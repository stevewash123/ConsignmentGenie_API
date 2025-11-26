import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Location } from '@angular/common';
import { RouterTestingModule } from '@angular/router/testing';
import { Component } from '@angular/core';
import { RegisterComponent } from './register.component';
import { By } from '@angular/platform-browser';

@Component({ template: '' })
class MockComponent { }

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let router: Router;
  let location: Location;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        RegisterComponent,
        RouterTestingModule.withRoutes([
          { path: 'register/owner', component: MockComponent },
          { path: 'register/provider', component: MockComponent },
          { path: 'login', component: MockComponent }
        ])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render ConsignmentGenie title', () => {
    const titleElement = fixture.debugElement.query(By.css('h1'));
    expect(titleElement.nativeElement.textContent).toContain('ConsignmentGenie');
  });

  it('should render "Join Consignment Genie" heading', () => {
    const headingElement = fixture.debugElement.query(By.css('h2'));
    expect(headingElement.nativeElement.textContent).toContain('Join Consignment Genie');
  });

  it('should render shop owner role card', () => {
    const ownerCard = fixture.debugElement.query(By.css('a[routerLink="/register/owner"]'));
    expect(ownerCard).toBeTruthy();

    const ownerTitle = ownerCard.query(By.css('h3'));
    expect(ownerTitle.nativeElement.textContent).toContain('Open a Consignment Shop');

    const ownerDescription = ownerCard.query(By.css('p'));
    expect(ownerDescription.nativeElement.textContent).toContain('→ Shop setup wizard');
  });

  it('should render provider role card', () => {
    const providerCard = fixture.debugElement.query(By.css('a[routerLink="/register/provider"]'));
    expect(providerCard).toBeTruthy();

    const providerTitle = providerCard.query(By.css('h3'));
    expect(providerTitle.nativeElement.textContent).toContain('Consign Items at a Shop');

    const providerDescription = providerCard.query(By.css('p'));
    expect(providerDescription.nativeElement.textContent).toContain('→ Basic signup');
  });

  it('should render login link', () => {
    const loginLink = fixture.debugElement.query(By.css('a[routerLink="/login"]'));
    expect(loginLink).toBeTruthy();
    expect(loginLink.nativeElement.textContent).toContain('Log In');
  });

  it('should display role icons', () => {
    const roleIcons = fixture.debugElement.queryAll(By.css('.role-icon'));
    expect(roleIcons.length).toBe(2);

    expect(roleIcons[0].nativeElement.textContent).toContain('🏪');
    expect(roleIcons[1].nativeElement.textContent).toContain('📦');
  });

  it('should have proper navigation links', () => {
    const ownerLink = fixture.debugElement.query(By.css('a[routerLink="/register/owner"]'));
    const providerLink = fixture.debugElement.query(By.css('a[routerLink="/register/provider"]'));
    const loginLink = fixture.debugElement.query(By.css('a[routerLink="/login"]'));

    expect(ownerLink.nativeElement.getAttribute('routerLink')).toBe('/register/owner');
    expect(providerLink.nativeElement.getAttribute('routerLink')).toBe('/register/provider');
    expect(loginLink.nativeElement.getAttribute('routerLink')).toBe('/login');
  });

  it('should render select buttons in role cards', () => {
    const selectButtons = fixture.debugElement.queryAll(By.css('.select-btn'));
    expect(selectButtons.length).toBe(2);

    selectButtons.forEach(button => {
      expect(button.nativeElement.textContent).toContain('Select');
    });
  });

  it('should have proper CSS classes for styling', () => {
    const registrationPage = fixture.debugElement.query(By.css('.registration-page'));
    expect(registrationPage).toBeTruthy();

    const container = fixture.debugElement.query(By.css('.container'));
    expect(container).toBeTruthy();

    const registrationCard = fixture.debugElement.query(By.css('.registration-card'));
    expect(registrationCard).toBeTruthy();

    const selectionSection = fixture.debugElement.query(By.css('.selection-section'));
    expect(selectionSection).toBeTruthy();

    const roleSelection = fixture.debugElement.query(By.css('.role-selection'));
    expect(roleSelection).toBeTruthy();

    const roleCards = fixture.debugElement.queryAll(By.css('.role-card'));
    expect(roleCards.length).toBe(2);

    const loginLinkDiv = fixture.debugElement.query(By.css('.login-link'));
    expect(loginLinkDiv).toBeTruthy();
  });

  it('should navigate to owner registration when owner card is clicked', async () => {
    const ownerLink = fixture.debugElement.query(By.css('a[routerLink="/register/owner"]'));
    ownerLink.nativeElement.click();

    await fixture.whenStable();
    expect(location.path()).toBe('/register/owner');
  });

  it('should navigate to provider registration when provider card is clicked', async () => {
    const providerLink = fixture.debugElement.query(By.css('a[routerLink="/register/provider"]'));
    providerLink.nativeElement.click();

    await fixture.whenStable();
    expect(location.path()).toBe('/register/provider');
  });

  it('should navigate to login when login link is clicked', async () => {
    const loginLink = fixture.debugElement.query(By.css('a[routerLink="/login"]'));
    loginLink.nativeElement.click();

    await fixture.whenStable();
    expect(location.path()).toBe('/login');
  });
});