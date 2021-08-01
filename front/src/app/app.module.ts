import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { SearchKeywordsComponent } from './components/comic-list/search-keywords/search-keywords.component';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { MaterialModule } from './modules/material.module';
import { ComicListComponent } from './components/comic-list/comic-list.component';
import { ReactiveFormsModule } from '@angular/forms';
import { MockInterceptor } from './_mock/mock.inteeceptor';
import { SalesDatePipe } from './components/comic-list/sales-date.pipe';
import { LicenseDialogComponent } from './components/license-dialog/license-dialog.component';
import { ImageSrcPipe } from './components/comic-list/image-src.pipe';
import { EventRegisterDialogComponent } from './components/event-register-dialog/event-register-dialog.component';
import { SelectServiceComponent } from './components/event-register-dialog/select-service/select-calendar.component';
import { SelectGoogleCalendarComponent } from './components/event-register-dialog/google/select-google-calendar/select-google-calendar.component';
import { RegisterGoogleCalendarComponent } from './components/event-register-dialog/google/register-google-calendar/register-google-calendar.component';

@NgModule({
  declarations: [
    AppComponent,
    SearchKeywordsComponent,
    ComicListComponent,
    SalesDatePipe,
    LicenseDialogComponent,
    ImageSrcPipe,
    EventRegisterDialogComponent,
    SelectServiceComponent,
    SelectGoogleCalendarComponent,
    RegisterGoogleCalendarComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    HttpClientModule,
    MaterialModule,
    ReactiveFormsModule
  ],
  providers: [
    // { provide: HTTP_INTERCEPTORS, useClass: MockInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
