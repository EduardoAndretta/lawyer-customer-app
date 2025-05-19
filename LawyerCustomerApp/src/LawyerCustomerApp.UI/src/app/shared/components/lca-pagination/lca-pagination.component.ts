import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-lca-pagination',
  templateUrl: './lca-pagination.component.html',
  styleUrls: ['./lca-pagination.component.css']
})
export class LcaPaginationComponent implements OnChanges {
  @Input() currentPage: number = 1;
  @Input() totalItems: number = 0;
  @Input() itemsPerPage: number = 10;
  @Input() maxPagesToShow: number = 5; // Max number of page links to show (e.g., 1 2 3 ... 10)

  @Output() pageChange = new EventEmitter<number>();

  totalPages: number = 0;
  pages: (number | string)[] = []; // Can include '...' for ellipsis

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['totalItems'] || changes['itemsPerPage'] || changes['currentPage']) {
      this.calculatePages();
    }
  }

  protected min(...values: number[]): number {
    return Math.min(...values);
  }

  private calculatePages(): void {
    if (this.totalItems <= 0 || this.itemsPerPage <= 0) {
      this.totalPages = 0;
      this.pages = [];
      return;
    }

    this.totalPages = Math.ceil(this.totalItems / this.itemsPerPage);
    this.pages = this.generatePageList(this.currentPage, this.totalPages, this.maxPagesToShow);
  }

  private generatePageList(currentPage: number, totalPages: number, maxPages: number): (number | string)[] {
    if (totalPages <= maxPages) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const pages: (number | string)[] = [];
    const halfMax = Math.floor(maxPages / 2);
    const canShowLeftEllipsis = currentPage - halfMax > 2;
    const canShowRightEllipsis = currentPage + halfMax < totalPages - 1;

    let startPage: number = 0;
    let endPage: number = 0;

    if (currentPage <= halfMax + 1) { // Near the beginning
        startPage = 1;
        endPage = maxPages - (canShowRightEllipsis ? 2 : 1); // -2 for '...' and last page, -1 if no right ellipsis
    } else if (currentPage >= totalPages - halfMax) { // Near the end
        startPage = totalPages - maxPages + (canShowLeftEllipsis ? 2 : 1);
        endPage = totalPages;
    } else { // In the middle
        startPage = currentPage - Math.floor((maxPages - (canShowLeftEllipsis ? 1 : 0) - (canShowRightEllipsis ? 1 : 0)) / 2);
        if (canShowLeftEllipsis) startPage = currentPage - Math.floor((maxPages - 2) / 2); // -2 for '1' and '...'
        if (canShowRightEllipsis) endPage = currentPage + Math.floor((maxPages - 2) / 2); // -2 for '...' and last page

        // Adjust if we don't have ellipsis on one side but could use more pages
        if (canShowLeftEllipsis && !canShowRightEllipsis) {
            endPage = Math.min(totalPages, startPage + maxPages - 2); // -2 for '1' and '...'
        } else if (!canShowLeftEllipsis && canShowRightEllipsis) {
            startPage = Math.max(1, endPage - maxPages + 2); // +2 for '...' and last page
        } else {
            startPage = currentPage - Math.floor((maxPages - (canShowLeftEllipsis ? 1 : 0) - (canShowRightEllipsis ? 1 : 0) - (canShowLeftEllipsis && canShowRightEllipsis ? 0 : 1) ) / 2) ;
            endPage = startPage + maxPages -1 - (canShowLeftEllipsis ? 1 : 0) - (canShowRightEllipsis ? 1 : 0) ;
        }
    }
    
    startPage = Math.max(1, startPage);
    endPage = Math.min(totalPages, endPage);


    if (canShowLeftEllipsis) {
        pages.push(1);
        pages.push('...');
    }

    for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
    }

    if (canShowRightEllipsis) {
        pages.push('...');
        pages.push(totalPages);
    }
    
    // Ensure pages are unique and sorted for edge cases if logic above isn't perfect
    const uniqueSortedPages = [...new Set(pages.filter(p => typeof p === 'number'))].sort((a,b) => (a as number) - (b as number));
    const resultPages: (number | string)[] = [];
    let lastPushed: number | undefined = undefined;

    if(totalPages <= maxPages){
        return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    // Simplified generation for when ellipses are involved
    // Always show first page
    resultPages.push(1);

    let rangeStart = Math.max(2, currentPage - Math.floor((maxPages - 2) / 2)); // -2 for first and last
    let rangeEnd = Math.min(totalPages - 1, currentPage + Math.floor((maxPages - 2) / 2));
    
    // Adjust range if it's too small due to being near start/end
    const currentRangeSize = rangeEnd - rangeStart + 1;
    const neededInRange = maxPages - 2; // excluding first and last page
    if (currentRangeSize < neededInRange) {
        if (currentPage < totalPages / 2) { // near start
            rangeEnd = Math.min(totalPages - 1, rangeEnd + (neededInRange - currentRangeSize));
        } else { // near end
            rangeStart = Math.max(2, rangeStart - (neededInRange - currentRangeSize));
        }
    }


    if (rangeStart > 2) {
        resultPages.push('...');
    }

    for (let i = rangeStart; i <= rangeEnd; i++) {
        if (i > 1 && i < totalPages) { // Don't add 1 or totalPages again
           resultPages.push(i);
        }
    }

    if (rangeEnd < totalPages - 1) {
        resultPages.push('...');
    }

    // Always show last page if more than one page
    if (totalPages > 1) {
        resultPages.push(totalPages);
    }

    // Remove duplicates that might occur due to simple logic
    return [...new Set(resultPages)];
  }


  goToPage(page: number | string): void {
    if (typeof page === 'number' && page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.pageChange.emit(page);
    }
  }

  onPrevious(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  onNext(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }
}