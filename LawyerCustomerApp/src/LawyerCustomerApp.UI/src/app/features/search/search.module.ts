import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { SearchRoutingModule } from './search-routing.module';
import { SearchResultsPageComponent } from './search-results-page/search-results-page.component';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [
    SearchResultsPageComponent
  ],
  imports: [
    CommonModule,
    SearchRoutingModule,
    SharedModule,
    RouterModule
  ],
  providers: []
})
export class SearchModule { }