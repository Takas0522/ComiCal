import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ComicListComponent } from './components/comic-list/comic-list.component';

const routes: Routes = [
  { path: '', component: ComicListComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
