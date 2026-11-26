import { Component, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Vial } from '../../services/vial.service';

// U-100 insulin syringes: 100 units = 1 ml.
const UNITS_PER_ML = 100;

/**
 * Works out how much to draw from a vial for a dose: ml = dose mg / (vial mg / vial ml), also
 * shown in U-100 syringe units. It fetches nothing: the host passes in the vial (and the dose
 * to start from), so it can be placed on any page that has a vial to hand.
 */
@Component({
  selector: 'app-vial-dose-calculator',
  imports: [FormsModule],
  templateUrl: './vial-dose-calculator.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./vial-dose-calculator.component.scss'],
})
export class VialDoseCalculatorComponent implements OnChanges {
  /** The vial to draw from, or null if none is saved. */
  @Input() vial: Vial | null = null;

  /** The dose to start with (e.g. the last dose taken), or null to start blank. */
  @Input() defaultDoseMg: number | null = null;

  doseMg: number | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    // Start again from the default when it changes or the vial is of another medication;
    // otherwise (e.g. the vial's amounts were re-saved) keep what the user has typed.
    const vialChange = changes['vial'];
    const otherMedication =
      vialChange &&
      vialChange.previousValue?.medicationName !== vialChange.currentValue?.medicationName;
    if (changes['defaultDoseMg'] || otherMedication) {
      this.doseMg = this.defaultDoseMg;
    }
  }

  /** The vial's concentration in mg/ml, or null without a vial. */
  get concentrationMgPerMl(): number | null {
    return this.vial ? this.vial.initialAmountMg / this.vial.initialVolumeMl : null;
  }

  /** The ml to draw for the dose, or null without a vial or a positive dose. */
  get drawMl(): number | null {
    const concentration = this.concentrationMgPerMl;
    const dose = this.doseMg;
    return concentration && dose != null && dose > 0 ? dose / concentration : null;
  }

  get drawUnits(): number | null {
    return this.drawMl === null ? null : this.drawMl * UNITS_PER_ML;
  }

  /** A number to at most the given decimal places, without trailing zeros (5, 2.5, 3.333). */
  trim(value: number, places: number): string {
    return String(Number(value.toFixed(places)));
  }
}
