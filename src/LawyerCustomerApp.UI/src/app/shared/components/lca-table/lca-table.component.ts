import { Component, Input, Output, EventEmitter, TemplateRef, ContentChildren, QueryList, AfterContentInit } from '@angular/core';

// For custom column templates
@Component({
  selector: 'lca-table-column',
  template: '<ng-content></ng-content>'
})
export class LcaTableColumnComponent {
  @Input() key!: string; // Key in the data object
  @Input() header!: string; // Header text for the column
  @Input()
  set template(template: TemplateRef<any> | undefined) { this._template = template; }
  get template(): TemplateRef<any> | undefined { return this._template; }
  private _template?: TemplateRef<any>;

  // If you want to pass template directly in HTML
  @ContentChildren(TemplateRef) contentTemplate?: QueryList<TemplateRef<any>>;

  ngAfterContentInit() {
    if (this.contentTemplate && this.contentTemplate.first && !this._template) {
        this._template = this.contentTemplate.first;
    }
  }
}


export interface LcaTableColumn {
  key: string; // Key in the data object for this column
  header: string; // Display name for the column header
  template?: TemplateRef<any>; // Optional custom template for cell rendering
  sortable?: boolean; // If the column is sortable
  cellClass?: string | ((item: any) => string); // Custom class for cells in this column
  headerClass?: string; // Custom class for header cell
}

@Component({
  selector: 'app-lca-table',
  templateUrl: './lca-table.component.html',
  styleUrls: ['./lca-table.component.css']
})
export class LcaTableComponent implements AfterContentInit {
  @Input() data: any[] = [];
  _columns: LcaTableColumn[] = [];

  @Input()
  set columns(cols: LcaTableColumn[]) {
    this._columns = cols;
  }
  get columns(): LcaTableColumn[] {
    return this._columns;
  }

  @Input() striped: boolean = false;
  @Input() bordered: boolean = false;
  @Input() hover: boolean = false;
  @Input() responsive: boolean = true; // For horizontal scrolling on small screens
  @Input() noDataMessage: string = "No data available.";

  // For declarative columns
  @ContentChildren(LcaTableColumnComponent) declaredColumns!: QueryList<LcaTableColumnComponent>;

  @Output() rowClick = new EventEmitter<any>(); // Emits the clicked row's data item

  ngAfterContentInit(): void {
    // If columns are declared via <lca-table-column>, use them
    if (this.declaredColumns && this.declaredColumns.length > 0 && this._columns.length === 0) {
      this._columns = this.declaredColumns.map(dc => ({
        key: dc.key,
        header: dc.header,
        template: dc.template
      }));
    }
  }

  onRowClick(item: any): void {
    this.rowClick.emit(item);
  }

  // Utility to get nested property value
  getProperty(item: any, key: string): any {
    if (!item || !key) return '';
    return key.split('.').reduce((o, k) => (o && o[k] !== 'undefined') ? o[k] : '', item);
  }

  getCellClass(item: any, column: LcaTableColumn): string {
    if (typeof column.cellClass === 'function') {
      return column.cellClass(item);
    }
    return column.cellClass || '';
  }
}